# Deferred Work

## Schema for Active Entries

Story 14.5 introduces a small structured-field block that each active entry should
carry so tools and reviewers do not have to infer status, source, or target from
arbitrary prose. The block is intentionally minimal and Markdown-readable; legacy
prose entries remain valid until they are explicitly migrated.

Required fields (one per line, anchored at the start of the indented sub-bullet):

- `ID:` — unique entry id, exactly as referenced elsewhere (e.g. `12.4-RV6`,
  `S11-FX`, `13.6-RV1`). Tests match the field value as a verbatim token; partial
  prose mentions and near-matches such as `12x4-RV6` or `112.4-RV6` do not count.
- `Status:` — one of `open`, `resolved`, `accepted`, or `carried-forward`. The
  vocabulary is closed and lowercase. Synonyms such as `done`, `closed`, `fixed`,
  or `deferred-again` are not allowed and will fail validation.
- `Source story:` — the story key, retro key, or review pass that produced the
  entry (for example `12-4-baseline-failures-sweep`).
- `Target artifact:` — the repository-relative path or planning artifact the entry
  targets. For release-lane baseline entries, this points at the consumer that
  owns the release filter (typically `tools/test-release.ps1` or a parser test).
- `Re-open trigger:` — one sentence describing the event or evidence that would
  re-open the entry. Required even for `resolved` and `accepted` so a future
  reviewer knows when the disposition no longer applies.
- One of `Evidence:` or `Rationale:` — `Evidence:` is required when `Status` is
  `resolved` and names the change (story, commit, test, or doc) that closes the
  risk; `Rationale:` is required when `Status` is `accepted` or `carried-forward`
  and explains why the risk remains intentionally.

Status semantics:

- `open` — planned action is still needed.
- `resolved` — code, test, or documentation evidence shows the risk no longer
  applies.
- `accepted` — the risk remains but is intentionally accepted with a written
  rationale.
- `carried-forward` — the risk remains and has been moved to a named future
  artifact, story, or trigger.

Optional fields (used only when relevant):

- `Test:` — fully-qualified `Class.Method` name when the entry is paired with a
  release-lane filter in `tools/test-release.ps1`. Required when `Target artifact`
  references the release-lane test script.

Historical entries that predate Story 14.5 do not carry the field block. Tooling
treats them as historical noise: they remain readable in code review and continue
to provide context, but the structured fields above are the source of truth for
parsers and planning of migrated entries.

## Story 24.5 Review Deferred Items (2026-07-06)

- source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md`
  summary: Case activity stream append and summary-hash update should be made atomic if duplicate retry side effects become observable.
  evidence: Story 24.5 bounded the stream and added summary backfill for missing legacy summaries, but `XADD` and summary hash updates remain separate Redis operations, so a failure between them can leave a read-model summary temporarily stale until rebuild/backfill.

  - ID: 24.5-CASE-ACTIVITY-ATOMIC-SUMMARY
  - Status: open
  - Source story: 24-5-hot-path-write-amplification-cleanup
  - Target artifact: src/Hexalith.Memories.Server/Cases/CaseActivityService.cs
  - Re-open trigger: duplicate case activity appends, stale failed-count/last-activity summaries, or Redis partial-write telemetry are observed after Story 24.5 ships.
  - Evidence: Review pass found `RecordEventAsync` writes the stream and summary hash separately; Story 24.5 mitigated missing legacy summaries with backfill but did not introduce Lua/transactional atomicity for this projection summary.

## Deferred Register Backlog Home Rollup (2026-06-30)

Sprint Change Proposal 2026-06-30 creates Epic 19 as the backlog home for active
deferred-register residuals that outlived completed epics.

- `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` target Story 19.2 unless the story
  explicitly accepts or reassigns them.
- `12.4-RV20` and `15.1-RV1` through `15.1-RV16` target Story 19.3 unless the
  story explicitly accepts or reassigns them.
- `15.2-RV1` through `15.2-RV9` and Story 15.3 migration-marker residuals
  target Story 19.4 unless the story explicitly accepts or reassigns them.
- Other active `open` or `carried-forward` entries are classified by Story 19.1
before implementation is scheduled.

## Story 20.5 Deferred Retention Slice (2026-07-04)

- **20.5-A41-ACCESS-TELEMETRY-RETENTION - carried-forward.** Audit finding A41 also requires a bounded retention/TTL policy for access telemetry. Story 20.5 implemented inbound request rate limiting and expanded mutating-operation audit emission, but retention is intentionally kept separate because access telemetry storage ownership and purge cadence need an operator-facing policy decision.

  - ID: 20.5-A41-ACCESS-TELEMETRY-RETENTION
  - Status: carried-forward
  - Source story: 20-5-inbound-rate-limiting-quotas-and-audit-completeness
  - Backlog home: Epic 27, registered Stories 27.1-27.4, plus the held C1 successor definitions in approved Sprint Change Proposal 2026-08-01. Story 27.3 owns C0 and independent C2/C3/C4 adapter qualification; all twenty-five C1 gates have no registered story owner; Story 27.4 owns deployment-shaped verification and close-out but remains `backlog` until compliant successor files are later registered and every C1 gate passes on its own evidence. Production lifecycle writes remain disabled and A41 remains open. Scheduling or a held proposal annex does not satisfy the resolution gate. **Corrected 2026-08-01 by approved Sprint Change Proposal 2026-08-01.**
  - Target artifact: `docs/dev/telemetry.md`, the Story 27.1 architecture decision, the selected access-telemetry sink/storage deployment and purge implementation, and focused lifecycle/tenant-privacy tests, or this entry updated to a complete explicit accepted-debt disposition.
  - Resolution gate: Keep this entry `carried-forward` and the matching sprint action `open` until bounded retention/TTL is implemented, documented, and validated, or accepted debt records a named approver and owner, affected storage/scope, rationale, risk and consequence, compensating controls, and a time-bounded review/expiry date or measurable reopen trigger.
  - Re-open trigger: Review before any claim that A41 is fully closed, before any production-retention assurance is made, and at the accepted-debt review/expiry trigger if that path is selected.
  - Rationale: Inbound quotas and audit completeness are implemented in Story 20.5; access telemetry retention remains unaddressed and is carried forward to avoid falsely closing the A41 retention requirement. Owner: operations maintainer / security remediation owner.

## Story 19.1 Classification Sweep (2026-06-30)

Story 19.1 classifies every active `open` / `carried-forward` structured entry (Story
14.5 schema) so completed epics do not hide unscheduled operational or consumer-risk
work. The active universe is 30 structured entries: 9 `open` (`15.2-RV1`…`15.2-RV9`)
and 21 `carried-forward` (`12.4-RV20`, `15.1-RV1`…`15.1-RV16`, `1.1-RR3`, `MEM-1`,
`MEM-2-ASPIRATE`, `MEM-3-OPENAPI`). Legacy prose bullets and `resolved`/`accepted`
entries are outside this AC1 universe. The 28 entries already routed by the Backlog
Home Rollup keep their existing blocks byte-for-byte and are recorded here as
**scheduled story**; only `1.1-RR3` and `MEM-1` were re-decided in place; three new
accepted-debt blocks are added below for the Epic 18 retrospective items that lived
only in prose. Completed Epics 15 and 18 are referenced, never reopened. [Source:
epics.md Story 19.1 ACs; sprint-change-proposal-2026-06-30.md; epic-18-retro-2026-06-25.md
Action Item 4]

| Active entry | Disposition | Home / cross-reference |
|---|---|---|
| `15.2-RV1` … `15.2-RV9` (open) | scheduled story | Story 19.4 (provider-registry / migration residual sweep) |
| `12.4-RV20` (carried-forward) | scheduled story | Story 19.3 (release-preflight / baseline-evidence sweep) |
| `15.1-RV1` … `15.1-RV16` (carried-forward) | scheduled story | Story 19.3 |
| `MEM-2-ASPIRATE` (carried-forward) | scheduled story | Story 19.2 (downstream contract artifact decisions) — story-id home for Action Item 4 |
| `MEM-3-OPENAPI` (carried-forward) | scheduled story | Story 19.2 — story-id home for Action Item 4 |
| `1.1-RR3` (carried-forward) | accept-until-trigger (kept carried-forward; owner named in its Rationale) | residual owned here; AppHost/release maintainer |
| `MEM-1` (carried-forward) | accept-until-trigger (kept carried-forward; trigger/owner/rationale refreshed) | residual non-reflectable Mcp `PackageId` half; Story 18.1 (done) test-enforces the reflectable half |
| real-Redis two-thread race (Story 18.4) | accepted debt — new block `18.4-REDIS-RACE` | this story — accepted-debt home for Action Item 4 |
| Dapr-sidecar pub/sub smoke (Story 18.8) | accepted debt — new block `18.8-DAPR-SMOKE` | this story — accepted-debt home for Action Item 4 |
| Story 18.4 token-anchoring edge | accepted debt — new block `18.4-TOKEN-EDGE` | this story — accepted-debt home for Action Item 4 |

Anti-over-promotion note (AC1): the three new entries plus `1.1-RR3` and `MEM-1` are
accept-until-trigger decisions, not scheduled implementation; only the 28 routed
entries carry a "schedule now" signal, and Stories 19.2/19.3/19.4 make the final
implement/accept/defer call for the IDs they own. The `15.3-RV*` migration-marker
items the Backlog Home Rollup names for Story 19.4 are legacy prose (no field block),
so they are out of this sweep's AC1 structured-entry scope and are left for Story 19.4.

### New accepted-debt entries (Epic 18 retrospective Action Item 4)

These three items existed only in Epic 18 retrospective prose. They are recorded here
as `accepted` (infra-lane-deferred) structured entries with explicit re-open triggers,
giving Action Item 4 its remaining three homes without reopening Epic 18.

- **18.4-REDIS-RACE - accepted.** Real two-thread Redis race test for the Story 18.4 atomic ingest-dedup reservation runs only in an Aspire/Testcontainers lane this sandbox cannot execute.

  - ID: 18.4-REDIS-RACE
  - Status: accepted
  - Source story: 19-1-deferred-register-active-entry-classification-sweep
  - Target artifact: tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs (real-Redis / Aspire-Testcontainers concurrency lane)
  - Re-open trigger: before any production claim about concurrent ingest, run the real two-thread Redis race wherever a Docker/Aspire lane is available, or that lane becomes runnable in CI.
  - Rationale: Story 18.4 is substitute-proven by a deterministic winner/loser reservation test and unit-proven today; the real two-thread Redis race is infra-lane-deferred because this sandbox cannot run the Docker/Aspire lane. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.4 / MEM-4]

- **18.8-DAPR-SMOKE - accepted.** Dapr-sidecar pub/sub smoke for cross-module event delivery (Story 18.8) runs only in an Aspire/Testcontainers lane this sandbox cannot execute.

  - ID: 18.8-DAPR-SMOKE
  - Status: accepted
  - Source story: 19-1-deferred-register-active-entry-classification-sweep
  - Target artifact: tests/Hexalith.Memories.IntegrationTests (Dapr-sidecar pub/sub smoke lane over /events/ingest)
  - Re-open trigger: before any production claim about cross-module event delivery, run the Dapr-sidecar pub/sub smoke wherever a Docker/Aspire lane is available, or that lane becomes runnable in CI.
  - Rationale: Story 18.8 is proven today by in-process HTTP E2E tests over `/events/ingest`; the Dapr-sidecar smoke is infra-lane-deferred because this sandbox cannot run the sidecar lane. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.8]

- **18.4-TOKEN-EDGE - accepted.** Story 18.4 token-anchoring edge: a token whose first use falls back to a pre-existing `sourceUri` unit relies on the 24h reservation key rather than the permanent dedup record.

  - ID: 18.4-TOKEN-EDGE
  - Status: accepted
  - Source story: 19-1-deferred-register-active-entry-classification-sweep
  - Target artifact: src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs (idempotency-token anchoring path)
  - Re-open trigger: a token whose first use falls back to a pre-existing `sourceUri` unit relying on the 24h reservation key (not a permanent record) causes a real dedup/idempotency defect, or a hardening story is scheduled.
  - Rationale: tokens augment and never replace the permanent source-URI dedup record, so the edge is a known narrow case accepted until it produces a real defect or a hardening story is scheduled. Owner: Amelia / release maintainer. [Source: epic-18-retro-2026-06-25.md Action Item 4; Story 18.4 / MEM-4]

## Story 19.2 Downstream Contract Artifact Decisions (2026-06-30)

Story 19.2 makes the final implement/accept/defer call for the two generated-artifact
carry-forwards that Story 19.1 routed here (the "story-id home" for Epic 18 retrospective
Action Item 4). Both are accepted as not needed for current consumers, because a maintained
operator/ACL-facing contract plus a build-failing drift-guard test already covers the need
and no consumer is blocked on generated output.

| ID | Decision | Maintained-doc + drift guard (AC3) | Re-open trigger |
|---|---|---|---|
| `MEM-2-ASPIRATE` | accepted (not needed now) | `docs/operations/deployment-configuration.md` + `DeploymentConfigurationContractTests` | a consumer needs ready-to-apply K8s/Dapr manifests emitted from the AppHost topology |
| `MEM-3-OPENAPI` | accepted (not needed now) | `docs/operations/route-surface.md` + `RouteSurfaceContractTests` | a consumer needs a generated OpenAPI/Swagger document for client/ACL generation |

Action Item 4 (Epic 18 retro) is already `done` (closed by Story 19.1 because both IDs had a
story-id home); converting them from `carried-forward` to `accepted` does not change its
done-condition, so no `sprint-status.yaml` action-item edit is made here. Completed Epics 15
and 18 are referenced, never reopened. [Source: epics.md Story 19.2 ACs;
sprint-change-proposal-2026-06-30.md Story 19.2; deferred-work.md Story 19.1 rollup]

## Story 19.3 Release Preflight and Baseline Evidence Decisions (2026-06-30)

Story 19.3 makes the final implement/accept/defer call for the 17 release-quality
carry-forwards that Story 19.1 routed here: `12.4-RV20` (strict baseline replay evidence)
and `15.1-RV1` ... `15.1-RV16` (the 15.1 release-preflight code-review residuals). All 17 are
bucketed accept-until-trigger with **no implement-now selection**, because none has a current
failure, a pulling consumer, or a blocked release, and each fix is a release-owner policy
decision rather than a clear patch -- promoting any of them now would violate the Epic 19
anti-over-promotion guardrail. The natural future-release-hardening-story homes below are
recorded so a triggered entry is pre-routed; no such story is scheduled now.

| Entry(ies) | Decision | Natural future home on trigger |
|---|---|---|
| `12.4-RV20` | accepted (ancestry-based proof sufficient; AC1) | a release-quality "Strict Release Baseline Replay Evidence" story, only if a post-mortem or quality story reopens it |
| `15.1-RV1`, `15.1-RV2`, `15.1-RV3`, `15.1-RV13`, `15.1-RV15` | accepted (accept-until-trigger) | release-preflight script robustness sweep (`tools/release-preflight.ps1`) |
| `15.1-RV4`, `15.1-RV5`, `15.1-RV6`, `15.1-RV7`, `15.1-RV8`, `15.1-RV9`, `15.1-RV14`, `15.1-RV16` | accepted (accept-until-trigger) | release test-helper hardening sweep (preflight pytest + `CiTestInventoryTests`) |
| `15.1-RV10`, `15.1-RV11`, `15.1-RV12` | accepted (accept-until-trigger) | docs/governance hygiene (`docs/dev/release-runbook.md`; deferred-register tooling) |

No implement-now item was selected, so AC3's focused-validation obligation (changed script +
workflow + inventory test) does not fire; only the `CiTestInventoryTests` deferred-work parser
guard was run. Completed Epics 12 and 15 (the entries' source stories) are referenced, never
reopened. [Source: epics.md Story 19.3 ACs; sprint-change-proposal-2026-06-30.md Story 19.3 +
risk note line 95; deferred-work.md Backlog Home Rollup + Story 19.1 classification table]

## Story 19.4 Provider Registry and Migration Residual Decisions (2026-06-30)

Story 19.4 makes the final implement/accept/defer call for the provider-registry and
migration-marker residuals the Backlog Home Rollup routed here: the nine `15.2-RV1` ...
`15.2-RV9` structured entries (Story 15.2 provider/model/dimension registry review) and the
twelve owned `15.3-RV*` legacy-prose migration-marker items (Story 15.3 live-migration
coordination review). Every decision below was taken after re-reading current code
(`EmbeddingProviderDefaults`, `EmbeddingClient`, `GenerateEmbeddingActivity`,
`EmbeddingVectorMigrationService`, `EmbeddingMigrationMarkerReader`,
`RedisEmbeddingMigrationStore`) and the operator docs. This story makes the final call under
the Backlog Home Rollup's "unless the story explicitly accepts or reassigns them" clause,
references completed Stories 15.2 and 15.3 as sources without reopening Epic 15, and preserves
the historical `15.3-RV*` prose unchanged (no new structured follow-up entry was scheduled).
**Implement-now selections: zero.** Because no implement-now provider or migration item was
selected, AC3's focused write-time-plus-read/runtime test obligation does not fire; only the
`CiTestInventoryTests` deferred-work parser guard was run. [Source: epics.md Story 19.4 ACs;
sprint-change-proposal-2026-06-30.md Story 19.4 + risk note line 95; deferred-work.md Backlog
Home Rollup + Story 19.1 classification table]

### Provider-registry residuals (`15.2-RV1` ... `15.2-RV9`) — AC1, AC3

All nine flip `open` -> `accepted` (accept-until-trigger), zero implement-now. The risks are
real but dormant: the closed registry currently holds one model per provider and exactly two
runtime providers (Google, Ollama), so cross-provider dispatch, persisted-casing, and
migration-target risks cannot fire today. Each entry's structured block carries the full
rationale, re-open trigger, and owner; this table is the at-a-glance final call.

| ID | Final decision | Natural future home on trigger |
|---|---|---|
| `15.2-RV1` | accepted (no implement-now) | contract casing/canonicalization story, only if the contract boundary changes |
| `15.2-RV2` | accepted (no implement-now) | provider-registry model-expansion tests when a second model lands under one provider |
| `15.2-RV3` | accepted (no implement-now) | registry-wide test-fixture hygiene sweep |
| `15.2-RV4` | accepted (no implement-now) | provider-runtime dispatch abstraction story for a third provider |
| `15.2-RV5` | accepted (no implement-now) | provider identifier canonicalization story covering write/read/migration equality |
| `15.2-RV6` | accepted (no implement-now) | migration target factory/registry story for a third provider |
| `15.2-RV7` | accepted (no implement-now) | provider validation UX cleanup if whitespace diagnostics become operator-visible |
| `15.2-RV8` | accepted (no implement-now) | operator visibility/remediation story for persisted invalid configs |
| `15.2-RV9` | accepted (no implement-now) | test-isolation sweep if the metric test flakes |

If any of these is later selected implement-now, the strongest coherent cluster is
`15.2-RV4` + `15.2-RV5` + `15.2-RV6` (runtime dispatch, persisted identifier casing, and
migration target selection), which must be solved together; AC3 then requires tests for both
write-time validation and read/runtime comparison paths where practical.

### Migration-marker residuals (Story 15.3 legacy prose) — AC2, AC3

The twelve owned `15.3-RV*` items stay as historical prose under the Story 15.3 review heading
(not migrated into structured blocks, since no follow-up story is scheduled). This table is the
forward-looking decision: which remain trigger-bound versus which become mandatory before the
next provider-migration investment.

| Legacy item(s) | Final decision | Required before next provider migration investment? |
|---|---|---|
| `15.3-RV6`, `15.3-RV8`, `15.3-RV10`, `15.3-RV13`, `15.3-RV22`, `15.3-RV24`, `15.3-RV26` | trigger-bound accepted risks | No — only if each item's own re-open trigger fires |
| `15.3-RV15`, `15.3-RV16`, `15.3-RV27` | migration-marker target-consistency cluster | Yes — bundle as a concrete target-consistency migration-hardening story before scheduling a new provider migration investment |
| `15.3-RV18`, `15.3-RV25` | operator-recovery and operator-copy cluster | Reassess before any production migration claim or operator-facing migration investment; accepted until then |

Current code confirms the cluster split: `RedisEmbeddingMigrationStore` completion does not
target-match the active marker and resume does not verify the active-marker target, and the
active-marker hash has no TTL — so the `15.3-RV15`/`15.3-RV16`/`15.3-RV27` target-consistency
cluster is the only clearly code-shaped migration-hardening story (detect an existing active
marker for another target, verify the completion target matches the active marker, and make
resume refuse drifted active-marker state). The `15.3-RV18`/`15.3-RV25` operator-recovery items
are documentation/alerting decisions, accepted until an operator escalation or the next
migration-investment story. The trigger-bound group stays dormant: the marker reader compares
provider/model with `OrdinalIgnoreCase` and stores/parses dimensions invariantly, the completed
active-marker hash is short-circuited by the reader's `status` check, and the keyed-Redis guard
is intentionally optional at the generate site while the indexing activities remain the
mandatory gate. Current `docs/operations/embedding-providers.md` already states the durable-marker
behavior, active-marker retry/failure semantics, and no global ingestion pause, so no doc change
is made here. [Source: deferred-work.md "Deferred from: code review of
15-3-live-migration-coordination-policy (2026-05-14)"; RedisEmbeddingMigrationStore.cs;
EmbeddingMigrationMarkerReader.cs; GenerateEmbeddingActivity.cs; docs/operations/embedding-providers.md]

## Story 15.5 Triage Rollup (2026-05-15)

Story 15.5 performed a bounded sweep rather than a full historical migration.
The selected set below contains entries that need active planning signal,
refreshed ownership, or an explicit risk decision before the next implementation
epic. Historical prose entries remain under their original headings for context.

- **12.4-RV20 - accepted.** Strict literal per-SHA replay evidence is a
  release-quality proof candidate, not a runtime defect. Ancestry-based HEAD-inheritance
  proof remains acceptable for existing close-out evidence; Story 19.3 declines to
  schedule a strict replay evidence story now.

  - ID: 12.4-RV20
  - Status: accepted
  - Source story: 15-5-deferred-register-triage-sweep
  - Target artifact: _bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md; tools/test-release.ps1; tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
  - Re-open trigger: A release post-mortem traces a regression to a test that existed at one of the named anchor SHAs but was silently fixed before HEAD, or a release-quality story explicitly requests strict literal replay evidence over ancestry-based proof.
  - Rationale: Story 19.3 (2026-06-30) reviewed the release-evidence need (AC1) and accepts ancestry-based HEAD-inheritance proof as sufficient for current close-out evidence, declining to create the proposed "Strict Release Baseline Replay Evidence" story until the re-open trigger fires. This governance sweep does not run historical checkout/build/test lanes or mutate release tooling. Owner: release maintainer.

- **12.6-RV5 - resolved.** The `EmbeddingInputContentKindTests` telemetry
  assertions now use per-test unique tenant ids, tenant-filtered captures, and a
  thread-safe capture queue, removing the dormant static-meter contamination risk
  that originally motivated the S11-FA release-lane baseline.

  - ID: 12.6-RV5
  - Status: resolved
  - Source story: deferred-work-implementation-2026-05-19
  - Target artifact: tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs
  - Re-open trigger: `EmbeddingInputContentKindTests` flakes again, or another story adds a concurrent `MemoriesMeter.EmbeddingApiCalls` assertion path that could share static meter captures.
  - Evidence: The focused telemetry tests now call `UniqueTenantId(...)`, capture only matching `tenant_id` measurements from `MemoriesMeter.EmbeddingApiCalls`, store observations in `ConcurrentQueue<(TenantId, ContentKind, Delta)>`, and assert a single matching metric event per test case.

- **Story-9.3-ProjectionRegistryCrossCheck - resolved.** Handler mismatch
  detection now has a repository-owned projection binding provider contract and
  emits `ProjectionBindingMissing` only when an authoritative tenant-scoped
  registry proves a configured route lacks a runtime projection binding.

  - ID: Story-9.3-ProjectionRegistryCrossCheck
  - Status: resolved
  - Source story: 16-1-projection-registry-cross-check-design
  - Target artifact: src/Hexalith.Memories.EventStore/IProjectionBindingProvider.cs; src/Hexalith.Memories.EventStore/ProjectionBinding.cs; src/Hexalith.Memories.EventStore/ProjectionBindingSnapshot.cs; src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs; src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs; tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs
  - Re-open trigger: A host needs automatic EventStore discovery adaptation, projection liveness or lag evidence, or authoritative registry detection beyond the host-provided tenant boundary.
  - Evidence: Story 16.1 adds `IProjectionBindingProvider`, default `Unknown` posture, authoritative-only `ProjectionBindingMissing` diagnostics, tenant-scoped deterministic matching, CLI/contract coverage, and operator documentation.

- **12.4-RV10 - accepted.** A parse-time warning for dropped bare-token bullets
  may help story authors, but the current out-of-scope-files diagnostic already
  catches the issue once a changed file lands outside the parsed allow-list.

  - ID: 12.4-RV10
  - Status: accepted
  - Source story: 15-5-deferred-register-triage-sweep
  - Target artifact: tools/check-story-file-scope.py
  - Re-open trigger: A contributor confusion incident or story-template redesign shows that pre-commit author warnings are needed before any changed-file validation runs.
  - Rationale: The value is low until there is evidence of author confusion, and adding parse-time stderr warnings could create noise for legitimate non-bullet prose.

- **12.4-RV11 - accepted.** Local Windows absolute-path cosmetic noise remains
  intentionally accepted because CI diagnostics use repository-relative paths and
  do not expose maintainer-visible drive letters.

  - ID: 12.4-RV11
  - Status: accepted
  - Source story: 15-5-deferred-register-triage-sweep
  - Target artifact: tools/check-story-file-scope.py
  - Re-open trigger: A PR review comment or release-evidence document cites a local Windows drive-letter path emitted by `tools/check-story-file-scope.py`, or a maintainer reports that pasting story-scope tooling output from a local Windows run into a shared review channel leaks a drive letter.
  - Rationale: The remaining issue is cosmetic and local-only; changing it now would add story-scope tooling churn without improving CI or reviewer evidence.

### Epic 14 Retrospective Reconciliation

- `S11-FC`, `12.1-RV3`, and `12.1-RV4` are already reconciled by Story 15.1:
  `S11-FC` and `12.1-RV4` are resolved, while `12.1-RV3` is accepted with a
  documented release-maintainer risk decision.
- `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, and `13.3-RV8` are already reconciled
  by Story 15.2 through the provider/model/dimension registry work.
- `13.6-RV1` and `13.6-RV3` are already reconciled by Story 15.3 through live
  migration marker enforcement and accepted migration result semantics.
- `13.2-RV4` is already reconciled by Story 15.4 through token endpoint transport
  policy enforcement and operations documentation.
- `13.7-RV4` is already resolved by the AppHost-owned `RepositoryRootLocator`
  structured entry dated 2026-05-12; no new backlog item is created here.
- The Epic 14 retrospective's "Preparation For The Next Work" note is stale
  because Epic 15 now exists. This rollup records the reconciliation instead of
  rewriting retrospective history.

### Follow-Up Story Proposals

1. **Strict Release Baseline Replay Evidence**

   Deferred IDs: `12.4-RV20`.
   Target artifacts: `_bmad-output/implementation-artifacts/12-4-baseline-failures-sweep.md`, `tools/test-release.ps1`, `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`, and run logs under the implementation artifact folder.
   Validation expectations: restore each named anchor SHA in isolated worktrees, run the authoritative release/test lanes available at that SHA, capture pass/fail evidence, and prove no tracked files or submodule pointers drift.
   Scope boundary: evidence-only quality proof; no release tooling behavior changes unless the replay exposes a real defect.

2. **Telemetry Test Isolation Hardening** — completed 2026-05-19.

   Deferred IDs: `12.6-RV5`.
   Target artifacts: `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs` and adjacent telemetry test helpers if needed.
   Validation expectations: make the test use unique tenant/source filtering and thread-safe capture mechanics, run focused `EmbeddingInputContentKindTests`, run the sibling `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` coverage, and run the relevant Server test slice.
   Resolution: implemented as a test-only cleanup in `EmbeddingInputContentKindTests`; no production telemetry contract change was needed.

3. **Projection Registry Cross-Check Design** — promoted to Story 16.1 on 2026-05-19.

   Deferred IDs: `Story-9.3-ProjectionRegistryCrossCheck`.
   Target artifacts: `src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs`, `src/Hexalith.Memories.Server/Handlers/HandlerRegistryService.cs`, `tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs`, and any projection-registry design note created by that story.
   Validation expectations: define the projection registry contract, prove mismatch detection compares routing declarations with actual tenant projection bindings, add negative tests for configured-but-unbound projections, and preserve existing handler mismatch CLI/API contracts.
   Scope boundary: architecture/design plus focused proof; do not retrofit broad server authentication or unrelated handler observation-window features.

## Deferred from: code review of 15-5-deferred-register-triage-sweep (2026-05-15)

- **15.5-RV1 — `git diff --check` validation claim is inaccurate.** Story 15.5 Dev Agent Record states `git diff --check ... passed with only expected LF-to-CRLF working-copy warnings`, but actual `git diff --check 9042c17..c2e575c` reports trailing-whitespace errors on `_15-5-review-diff.patch`. Re-open trigger: any future story's validation block reuses the same tolerance wording without verifying `--check` output is genuinely error-free.
- **15.5-RV2 — `sprint-status.yaml:last_updated` not advanced when post-implementation commit `c2e575c` landed.** Cosmetic drift; the dev-story timestamp `2026-05-15T12:45:15+02:00` predates the 15:55 follow-on commit. Re-open trigger: a tool starts treating `last_updated` as a freshness proxy across all commits on a story.
- **15.5-RV3 — Task 3 prose "`13.1-RV6` and related provider work" is looser than the rollup's explicit 4-ID enumeration.** `15-5-deferred-register-triage-sweep.md:79` lists one ID; `deferred-work.md:119` enumerates `13.1-RV6`, `13.1-RV10`, `13.1-RV11`, `13.3-RV8`. Rollup is accurate; task description was provisional. Re-open trigger: a future story re-reads Task 3 prose as a complete ownership list and misses one of the four IDs.
- **15.5-RV4 — `Target artifact:` field uses `;`-joined multi-paths in `12.4-RV20` and `Story-9.3-ProjectionRegistryCrossCheck` blocks.** `deferred-work.md:68,92` — the Story 14.5 schema describes `Target artifact:` as singular, but Story 15.4's `13.2-RV4` (line 178) and Story 15.2's HybridSearch entry (line 293) already use multi-path joined formats and the `CiTestInventoryTests` parser tolerates them (48/48 PASS). Pre-existing pattern; a Story-14.5-owned schema-cleanliness pass should either tighten the parser to require single-path values or formalize a multi-path separator. Re-open trigger: a parser regression where the joined string is treated as one literal path and a target-artifact filter misses a real consumer.

## Deferred from: code review of 15-4-token-endpoint-transport-policy (2026-05-15)

- **15.4-RV1 — Sanitization-message assertions are tautological.** `OidcTokenProviderTests.cs:656-669` and `EmbeddingProviderDefaultsTests.cs:876-889` — the positive `ShouldContain("HTTPS")`/`("loopback")`/`("localhost")`/`("127.0.0.1")`/`("[::1]")` assertions in `AssertSanitizedTransportPolicyMessage` re-state the constant the implementation throws and provide zero discrimination beyond confirming the exception is reached. The actual non-leak safety is enforced by `ShouldNotContain(endpoint)` and the dedicated `Bearer`/`abc.def.ghi`/`client-secret-value` checks. Re-open trigger: any test-hardening sweep that strengthens negative-content assertions across the server test suite, or a regression where the implementation changes the user-facing exception text and the test misses the divergence.

## Closed/Accepted by: Story 15.4 Token Endpoint Transport Policy (2026-05-14)

- **13.2-RV4 - resolved.** OIDC token endpoint validation now enforces HTTPS for production and
  permits `http://` only for literal local loopback hosts (`localhost`, `127.0.0.1`, and `[::1]`).
  The same policy is applied before tenant/default config persistence and before direct
  `IOidcTokenProvider` token acquisition, with sanitized errors that do not echo full endpoint URLs.

  - ID: 13.2-RV4
  - Status: resolved
  - Source story: 15-4-token-endpoint-transport-policy
  - Target artifact: src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs; src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs; docs/operations/embedding-providers.md
  - Re-open trigger: Any non-loopback `http://` OIDC token endpoint reaches tenant config persistence, direct token acquisition, an outbound token HTTP request, logs, or snapshots without being rejected by the HTTPS/local-loopback policy.
  - Evidence: Story 15.4 added `OidcTokenProvider.ValidateTokenEndpointTransport(...)`, reused it from `EmbeddingProviderDefaults.ValidateOptionalHttpUrl(...)` for `OidcTokenEndpoint`, documented the production HTTPS/local loopback exception in `docs/operations/embedding-providers.md`, and added focused `OidcTokenProviderTests` plus `EmbeddingProviderDefaultsTests` coverage for accepted loopback HTTP, rejected public/private/link-local/Docker/DNS-alias/127.0.0.2 HTTP, no-request-before-rejection, and non-leaking error text.

## Closed/Accepted by: Story 15.3 Live Migration Coordination Policy (2026-05-14)

- **13.6-RV1 - resolved.** Live migration cutover now writes a durable
  tenant-scoped active marker before index recreation or tenant config update, and
  runtime ingestion/indexing reads that marker to block stale provider/model
  writes for the migrating tenant.

  - ID: 13.6-RV1
  - Status: resolved
  - Source story: 15-3-live-migration-coordination-policy
  - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs
  - Re-open trigger: A production or test migration completes while raw or natural-language semantic hashes for the tenant contain a provider/model/dimensions tuple different from the active migration target after cutover.
  - Evidence: Story 15.3 added active marker writes in `RedisEmbeddingMigrationStore.StartMigrationMarkerAsync`, read/write guards in `GenerateEmbeddingActivity`, `IndexSemanticActivity`, and `IndexNaturalLanguageSemanticActivity`, plus focused tests proving old-provider generation and raw/NL semantic writes are blocked while the marker is active.

- **13.6-RV2 - resolved.** `IndexSemanticActivity.cs` now carries the standard
  ITANEO MIT copyright header because Story 15.3 touched the file
  substantively for the mandatory semantic write guard.

  - ID: 13.6-RV2
  - Status: resolved
  - Source story: 15-3-live-migration-coordination-policy
  - Target artifact: src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs
  - Re-open trigger: A future hand-written C# source file touched by a story lacks the standard project copyright header.
  - Evidence: Story 15.3 added the missing copyright header while updating `IndexSemanticActivity` for active migration marker enforcement.

- **13.6-RV3 - accepted.** The migration command keeps its local nullable
  string/tuple helper shape for `ValidateOptions(...)` and
  `TryBuildTargetConfig(...)`, with `EmbeddingMigrationResult` plus stable exit
  codes as the project-approved equivalent for this command surface.

  - ID: 13.6-RV3
  - Status: accepted
  - Source story: 15-3-live-migration-coordination-policy
  - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
  - Re-open trigger: Migration errors need `ApplicationError` metadata beyond a flat operator message, or `Hexalith.Memories.Server` adopts `Hexalith.Commons.ValueOrError<T>` as an approved dependency across this boundary.
  - Rationale: The helper results are private to `EmbeddingVectorMigrationService`, immediately converted into `EmbeddingMigrationResult`, and already produce automation-readable `Plumbing`, `DomainError`, and `Cancelled` exit codes with sanitized messages. Introducing `Hexalith.Commons.ValueOrError<T>` here would add cross-project reference churn without improving the public migration command contract.

## Closed/Accepted by: Story 15.2 Provider Model Dimension Registry (2026-05-13)

- **13.1-RV6 - resolved.** Provider validation now has a shared maximum
  vector-dimension policy and rejects out-of-policy dimensions before any tenant
  state or index path can consume them.

  - ID: 13.1-RV6
  - Status: resolved
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
  - Re-open trigger: A future registry entry accepts a vector dimension above the shared maximum without a story that explicitly raises the storage/memory policy, or a tenant config with `Dimensions = int.MaxValue` reaches persistence/index creation.
  - Evidence: Story 15.2 added `MaxSupportedDimensions = 16_384` in `EmbeddingProviderDefaults`, validates dimensions before model-specific allowlist checks, and added `EmbeddingProviderDefaultsTests.Validate_DimensionsAboveSharedMaximum_ShouldThrowAtConfigTime`.

- **13.1-RV10 - accepted.** Provider and model validation remains
  case-insensitive for compatibility, and caller-provided casing is preserved
  rather than normalized at validation time.

  - ID: 13.1-RV10
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
  - Re-open trigger: A persisted mixed-case provider/model value causes runtime dispatch, reindex detection, search metadata, or migration state comparisons to diverge.
  - Rationale: Ollama model tags may be case-sensitive outside the committed `qwen3-embedding:4b` model, so Story 15.2 keeps validation case-insensitive while preserving original values. Evidence is pinned by `EmbeddingProviderDefaultsTests.Validate_MixedCaseProviderAndModel_ShouldUseCaseInsensitiveRegistryLookup`; compatibility consumers continue to use `OrdinalIgnoreCase` where provider/model equality matters.

- **13.1-RV11 - resolved.** Provider/model/dimension validation now uses a
  closed provider-scoped registry, so cross-pollinated and unknown models fail
  by construction.

  - ID: 13.1-RV11
  - Status: resolved
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
  - Re-open trigger: Any provider/model pair validates without being present in the local registry, or a provider falls back to another provider's defaults, dimensions, models, or rate-limit ceiling.
  - Evidence: Story 15.2 replaced scattered provider/model/dimension/rate-limit checks with a single local registry in `EmbeddingProviderDefaults` and added `EmbeddingProviderDefaultsTests.Validate_CrossProviderModelPairs_ShouldThrow`, `Validate_UnknownModelForProvider_ShouldThrowAndListProviderModels`, `Validate_SyntacticallyValidButUnregisteredModel_ShouldThrow`, and provider-scoped rate-limit tests.

- **13.3-RV8 - accepted.** The persisted provider/model parser continues to
  lowercase the provider and preserve the model string after the first colon.

  - ID: 13.3-RV8
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs
  - Re-open trigger: A persisted provider/model identifier with mixed casing fails a real runtime path, migration path, or equality comparison because provider and model casing are handled asymmetrically.
  - Rationale: Provider names are registry keys and safe to normalize for dispatch, while model tags can contain embedded colons and may be case-sensitive in provider-specific APIs. Story 15.2 pins the behavior with `EmbeddingClientTests.ParseEmbeddingProvider_NormalizesProviderAndPreservesModelAfterFirstColon` and leaves runtime parsing unchanged.

## Deferred from: code review of 15-2-provider-model-dimension-registry (2026-05-14)

Items below were surfaced by the 3-layer adversarial review (Blind Hunter +
Edge Case Hunter + Acceptance Auditor) of commit `57819b4` but are outside the
story's File Scope or out of immediate fix range.

- **15.2-RV1 - accepted.** AC5 contract-tier serialization test not updated.

  - ID: 15.2-RV1
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: tests/Hexalith.Memories.Contracts.Tests/V1/TenantEmbeddingConfigSerializationTests.cs
  - Re-open trigger: A casing/canonicalization change at the contract boundary is later considered.
  - Rationale: Task 2 chose `accepted` for casing semantics, so contract-tier serialization is intentionally unchanged. Recorded for traceability against AC5's "contract/server tests cover ... deferred-work dispositions" wording. Story 19.4 (2026-06-30) re-reviewed the current contract serializer against live code and accepts this until the contract boundary changes: the contract tests still preserve JSON shape and value round-trip, and provider/model validation lives in server/provider paths, not the contract serializer, so no contract-tier change is warranted now. Natural future home: a contract casing/canonicalization story, only if the contract boundary changes. Owner: Contracts / Server maintainer.

- **15.2-RV2 - accepted.** Actor reindex tests lost same-Provider/different-Model isolation.

  - ID: 15.2-RV2
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs
  - Re-open trigger: Registry adds a second model under Google or Ollama, OR `GetBreakingChangeFields` regresses to flag only provider changes.
  - Rationale: Tests now switch Provider AND Model (Google→Ollama) instead of Model-only; same-provider/model-change reindex-trigger coverage cannot be restored inside this story because the closed registry currently lists exactly one model per provider. Story 19.4 (2026-06-30) confirmed the closed registry still lists exactly one model per provider (Google + Ollama only), so same-provider/different-model reindex coverage stays trigger-bound and accepted until a supported provider gains a second model or `GetBreakingChangeFields(...)` regresses. Natural future home: provider-registry model-expansion tests when a second model lands under one provider. Owner: Server test maintainer.

- **15.2-RV3 - accepted.** Other test files still use unregistered model literals.

  - ID: 15.2-RV3
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs, tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs
  - Re-open trigger: The covered code paths add a Validate-preflight, OR a registry-wide test-fixture-hygiene sweep is scheduled.
  - Rationale: `HybridSearchServiceTests` (line 74) and `TenantEmbeddingConfigEndpointTests` (lines 30, 39) use `"text-embedding-004"` / `"different-model"` literals that compile only because those tests do not call `Validate`. Pre-existing; out of File Scope. Story 19.4 (2026-06-30) accepts this as fixture-hygiene debt: the literals remain harmless until those paths begin validating tenant configs or a registry-wide test-fixture sweep is scheduled. Natural future home: a registry-wide test-fixture hygiene sweep. Owner: Server test maintainer.

- **15.2-RV4 - accepted.** `EmbeddingClient.IsGoogle/IsOllama` dispatch is hardcoded.

  - ID: 15.2-RV4
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs
  - Re-open trigger: Registry adds a third provider, OR an operator reports a failed identifier parse for a registered provider/model pair.
  - Rationale: Closed-allowlist behavior of `EmbeddingProviderDefaults` does not extend to `EmbeddingClient.ParseEmbeddingProviderIdentifier` or the dispatch site, which binary-check `IsGoogle || IsOllama`. Architectural follow-up; out of this story's File Scope. Story 19.4 (2026-06-30) verified `EmbeddingClient` still dispatches via `IsGoogle`/`IsOllama` and parses only those two providers; this is safe while the registry holds two providers and stays accepted until a third provider is added or an operator reports a failed parse for a registered pair. It is the strongest implement-now cluster with `15.2-RV5` and `15.2-RV6` (runtime dispatch, persisted identifier casing, migration target selection) and must be solved together, not in isolation. Natural future home: a provider-runtime dispatch abstraction story for a third provider. Owner: Server / ingestion maintainer.

- **15.2-RV5 - accepted.** `GenerateEmbeddingActivity` may emit mixed-case provider in persisted identifier.

  - ID: 15.2-RV5
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs
  - Re-open trigger: A tenant persists `Provider = "Google"` (mixed case, now accepted by Validate) and an equality comparison or migration state diverges from the lowercased parsed form.
  - Rationale: Tenant-persisted casing is preserved, but `ParseEmbeddingProviderIdentifier` lowercases the provider on read — write and read forms can diverge. Related to 15.2-RV4. Out of File Scope. Story 19.4 (2026-06-30) confirmed `GenerateEmbeddingActivity` writes the raw `$"{config.Provider}:{config.Model}"` form while the parser lowercases the provider, and the migration marker guard compares with `OrdinalIgnoreCase`, so this is accepted compatibility today, not a current defect. Accepted until a casing-sensitive equality or migration-state divergence is observed. Natural future home: a provider identifier canonicalization story covering write/read/migration equality (bundled with `15.2-RV4`/`15.2-RV6`). Owner: Server / ingestion maintainer.

- **15.2-RV6 - accepted.** Migration tool uses binary Google/Ollama coin-flip, not registry.

  - ID: 15.2-RV6
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs
  - Re-open trigger: Registry adds a third provider, OR a migration plan needs to target a new provider.
  - Rationale: `TargetProvider` defaults to `Ollama()` if not Google (lines 125-149). Related to 15.2-RV4. Out of File Scope. Story 19.4 (2026-06-30) confirmed `EmbeddingVectorMigrationService.TryBuildTargetConfig(...)` still chooses Google defaults for a Google target and Ollama defaults otherwise, then relies on `EmbeddingProviderDefaults.Validate(...)` to reject unsupported providers — safe for two providers but a binary defaulting path, accepted until a third provider or a new migration target lands. Natural future home: a migration target factory/registry story for a third provider (bundled with `15.2-RV4`/`15.2-RV5`). Owner: Server / migration maintainer.

- **15.2-RV7 - accepted.** Whitespace-prefixed provider not trimmed before registry lookup.

  - ID: 15.2-RV7
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs
  - Re-open trigger: Operator reports an unhelpful "Provider ' google' is not supported" error and the registry path needs to suggest the whitespace cause.
  - Rationale: `ArgumentException.ThrowIfNullOrWhiteSpace` accepts `" google"`, then `FindProvider` misses (no trim). Already family-deferred as `13.1-RV4`; surfaces again in the registry path. Story 19.4 (2026-06-30) accepts this until an operator UX issue justifies whitespace-specific diagnostics in `EmbeddingProviderDefaults.Validate(...)`. Natural future home: a provider validation UX cleanup if whitespace diagnostics become operator-visible. Owner: Server / ingestion maintainer.

- **15.2-RV8 - accepted.** Already-persisted invalid configs not surfaced on read.

  - ID: 15.2-RV8
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs
  - Re-open trigger: Operator needs visibility into tenants whose persisted config no longer validates under the closed registry.
  - Rationale: Closed-registry validation runs on write only — tenants whose state was valid under loose rules continue to be served. Story 15.2 documents this as intentional compatibility behavior; operator-visibility design is a follow-up. Story 19.4 (2026-06-30) accepts this until operator visibility for already-persisted invalid configs is explicitly needed; per the scope boundary, no read-time tenant-config rejection is added without an approved operator remediation path. Natural future home: an operator visibility/remediation story for persisted invalid configs. Owner: Server / ingestion maintainer.

- **15.2-RV9 - accepted.** "Order-sensitive metric test passed in isolation" acknowledged.

  - ID: 15.2-RV9
  - Status: accepted
  - Source story: 15-2-provider-model-dimension-registry
  - Target artifact: tests/Hexalith.Memories.Server.Tests (test ordering)
  - Re-open trigger: The order-sensitive metric test fails intermittently in CI, OR a test-isolation sweep is scheduled.
  - Rationale: Acknowledged in the Dev Agent Record but not fixed; not caused by this story. Story 19.4 (2026-06-30) accepts this until the order-sensitive metric test flakes in CI or a test-isolation sweep is scheduled. Natural future home: a test-isolation sweep if the metric test flakes. Owner: Server test maintainer.

## Closed/Accepted by: Story 15.1 Release Edge-Case Preflight Hardening (2026-05-13)

- **S11-FC - resolved.** Release execution now has a repository-owned stale-tag
  preflight before `npx semantic-release`. The script obtains the next version
  from semantic-release dry-run output, applies `.releaserc.json` `tagFormat:
  "v${version}"`, and checks exact local and remote refs before prepare or
  publish hooks can run.

  - ID: S11-FC
  - Status: resolved
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tools/release-preflight.ps1
  - Re-open trigger: semantic-release output changes so `tools/release-preflight.ps1` can no longer parse the next release version, or a release post-mortem shows a stale tag reached the publish-capable `npx semantic-release` step.
  - Evidence: Story 15.1 added `tools/release-preflight.ps1`, wired `.github/workflows/release.yml` to run it before `npx semantic-release`, and added `tests/tooling/release_preflight/release_preflight_test.py` coverage for no tag, local-only collision, remote-only collision, matching local/remote collision path, no-release dry-run output, and similarly prefixed non-colliding refs.

- **12.1-RV3 - accepted.** The repository removed its partial job-level
  `github.event.head_commit.message` skip parser and documents GitHub's native
  push skip handling as the release contract. The remaining edge is accepted
  because a workflow skipped by GitHub before job creation cannot run an
  in-workflow repository validator.

  - ID: 12.1-RV3
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: docs/dev/release-runbook.md
  - Re-open trigger: first silently skipped release caused by a bracketed skip instruction in a release-eligible merge/squash commit message, or GitHub exposes a pre-job policy hook that can reject such commits before native skip handling suppresses the workflow.
  - Rationale: Release maintainer ownership remains with the final merge/squash message author and reviewer. Story 15.1 makes the outcome predictable by removing the repository's partial parser, adding `CiTestInventoryTests.ReleaseWorkflow_ReleaseJob_DoesNotUseHeadCommitSkipCondition`, and documenting that bracketed skip instructions anywhere in the final commit message can suppress release. Accepted until 2026-08-13 unless the re-open trigger fires sooner.

- **12.1-RV4 - resolved.** The release restore contract is now explicitly
  verified: `package-lock.json` is tracked, matches root `package.json` for
  `npm ci`, and the workflow installs release tooling through `npm ci`.

  - ID: 12.1-RV4
  - Status: resolved
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: package-lock.json
  - Re-open trigger: `npm ci --ignore-scripts` fails from an isolated checkout/worktree, `package-lock.json` is removed from git tracking, or `.github/workflows/release.yml` stops using `npm ci` for release tooling restore.
  - Evidence: Story 15.1 confirmed `git ls-files -- package-lock.json package.json` lists both files, added `CiTestInventoryTests.ReleaseWorkflow_InstallReleaseTooling_UsesNpmCi`, documented the `npm ci` lockfile contract in `docs/dev/release-runbook.md`, and validated the fresh-clone-style restore with `npm ci --ignore-scripts` in an isolated worktree. Fresh-clone proof: `npm ci --ignore-scripts` run in working directory `D:\Hexalith.Memories` after deleting any pre-existing `node_modules/`; the command resolved the tracked `package-lock.json` against `package.json` without writing back to either file; post-run `git status -- package-lock.json package.json` reported zero changes.

## Deferred from: code review of 15-1-release-edge-case-preflight-hardening (2026-05-13)

Carry-forward findings from the 3-layer adversarial code review on 2026-05-13. Each entry uses the Story 14.5 schema; status is `carried-forward` unless noted.

- **15.1-RV1 - accepted.** Transient network failure in `Test-RemoteTagCollision` aborts the release lane.
  - ID: 15.1-RV1
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tools/release-preflight.ps1
  - Re-open trigger: A release attempt fails because `git ls-remote` returns a transient network/DNS error and the preflight has no retry/backoff.
  - Rationale: The preflight currently has no retry. A DNS hiccup turns a recoverable error into a hard abort. Deferred because the right policy (number of retries, backoff window, idempotency boundary) is a release-owner decision rather than a clear patch. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no release has failed on it; the retry policy is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.

- **15.1-RV2 - accepted.** Dry-run version regex hard-codes English semantic-release output.
  - ID: 15.1-RV2
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tools/release-preflight.ps1
  - Re-open trigger: semantic-release ever rewords `The next release version is X.Y.Z` or ships i18n output that no longer matches the regex, and the preflight starts mis-detecting the next version.
  - Rationale: Bound to current semantic-release output. A more stable contract would be `semantic-release --dry-run --debug` JSON or a plugin hook, but switching is out of scope for Story 15.1. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.

- **15.1-RV3 - accepted.** Final `catch` block loses inner-exception and stack trace.
  - ID: 15.1-RV3
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tools/release-preflight.ps1
  - Re-open trigger: A release failure investigation requires the original stack/inner exception and the operator only has the truncated `Write-Error -Message $_.Exception.Message` output.
  - Rationale: `Write-Error -Message $_.Exception.Message` discards inner exception and stack. Switch to `Write-Error -ErrorRecord $_` or `$_.Exception.ToString()` when a future release post-mortem proves the loss matters. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.

- **15.1-RV4 - accepted.** `CiTestInventoryTests` workflow-string assertions are brittle to cosmetic edits.
  - ID: 15.1-RV4
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
  - Re-open trigger: A future workflow edit needs `npm ci --ignore-scripts`, a renamed step name, or `pwsh ./tools/release-preflight.ps1` invocation form, and the existing strict `ShouldBe` assertions fail without a real contract violation.
  - Rationale: Exact-match `ShouldBe` for `Run`, `Name`, and `Shell` is consistent with the rest of `CiTestInventoryTests`. Loosening to `ShouldContain`/`ShouldStartWith` should be done as a sweep across the file, not in isolation. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.

- **15.1-RV5 - accepted.** Windows tempdir cleanup can raise `PermissionError`.
  - ID: 15.1-RV5
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tests/tooling/release_preflight/release_preflight_test.py
  - Re-open trigger: CI or a Windows developer hits intermittent `PermissionError` on tempdir cleanup because git keeps an index lock open when the test ends.
  - Rationale: Tests currently pass locally and in CI. Switching to `tempfile.TemporaryDirectory(ignore_cleanup_errors=True)` (Py 3.10+) is a one-line hardening but Story 15.1 has no evidence the path manifests yet. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.

- **15.1-RV6 - accepted.** `Path | None` union syntax requires Python 3.10+.
  - ID: 15.1-RV6
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tests/tooling/release_preflight/release_preflight_test.py
  - Re-open trigger: A contributor runs the test on Python 3.9 (or the project lowers its minimum) and gets a `TypeError` on test collection.
  - Rationale: Current CI runs Python 3.11+. Lowering to `Optional[Path]` would broaden compatibility but is not needed today. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.

- **15.1-RV7 - accepted.** Non-UTF-8 Windows codepage may raise `UnicodeDecodeError` on subprocess output.
  - ID: 15.1-RV7
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tests/tooling/release_preflight/release_preflight_test.py
  - Re-open trigger: A test runner uses a non-UTF-8 codepage and `pwsh` stderr contains non-ASCII characters, raising `UnicodeDecodeError` on `subprocess.run(..., text=True)`.
  - Rationale: Pass `encoding='utf-8', errors='replace'` to `subprocess.run`. Deferred as low-impact hardening; CI codepages are UTF-8. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.

- **15.1-RV8 - accepted.** `git init` default branch depends on host `init.defaultBranch`.
  - ID: 15.1-RV8
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tests/tooling/release_preflight/release_preflight_test.py
  - Re-open trigger: A future test that relies on the default branch name (rather than just tags) fails on a runner with a non-standard `init.defaultBranch` value.
  - Rationale: Current tests only push tags, so the default-branch name is irrelevant. Adding `--initial-branch=main` would future-proof the helper. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.

- **15.1-RV9 - accepted.** Test runner hardcodes `pwsh` without availability guard.
  - ID: 15.1-RV9
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tests/tooling/release_preflight/release_preflight_test.py
  - Re-open trigger: A non-Windows developer environment without PowerShell 7 runs `pytest`/`unittest discover` and sees a confusing `FileNotFoundError` instead of a clear skip.
  - Rationale: Add `@unittest.skipUnless(shutil.which('pwsh'), 'pwsh required')`. Deferred because CI and dev environments today all have pwsh. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.

- **15.1-RV10 - accepted.** Runbook release-day checklist renumbered to 17 items; other docs may reference old step numbers.
  - ID: 15.1-RV10
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: docs/dev/release-runbook.md
  - Re-open trigger: A maintainer follows a stale "see step 7" cross-reference in CONTRIBUTING.md or another doc that no longer matches the renumbered checklist.
  - Rationale: A repo-wide `rg "step 7|step 8|step 9" docs/` sweep is warranted but out of scope for Story 15.1. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.

- **15.1-RV11 - accepted.** `S11-FC` re-open trigger names `tools/release-preflight.ps1` by path.
  - ID: 15.1-RV11
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: _bmad-output/implementation-artifacts/deferred-work.md
  - Re-open trigger: The preflight script is renamed or relocated and the `S11-FC` re-open trigger silently fails to reference the right artifact.
  - Rationale: Use a more stable artifact phrasing like "the repository-owned release preflight script in `tools/`" if the script ever moves. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.

- **15.1-RV12 - accepted.** `12.1-RV3` accepted-until 2026-08-13 has no automated reminder.
  - ID: 15.1-RV12
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: _bmad-output/implementation-artifacts/deferred-work.md
  - Re-open trigger: Accepted-until date `2026-08-13` passes with no review surfacing the expired entry.
  - Rationale: No infrastructure today surfaces expired `accepted` entries. A scheduled check would close the gap. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: docs/governance hygiene sweep. Owner: release maintainer.

- **15.1-RV13 - accepted.** `git show-ref --verify` allowed exit codes do not cover `128`.
  - ID: 15.1-RV13
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tools/release-preflight.ps1
  - Re-open trigger: A release attempt fails with the generic "git failed with exit code 128" wrapper because the ref store is corrupt or otherwise unreadable.
  - Rationale: A clearer "ref-state probe failed" diagnostic would shorten release-day investigation. Current wrapper is acceptable for the common case. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.

- **15.1-RV14 - accepted.** Peeled-only-ref remote response not fixtured.
  - ID: 15.1-RV14
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tests/tooling/release_preflight/release_preflight_test.py
  - Re-open trigger: A real remote returns only peeled refs (`refs/tags/vX.Y.Z^{}`) without the unpeeled entry and the contract is not test-fixtured.
  - Rationale: The script accepts either, but no fixture proves the peeled-only path. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.

- **15.1-RV15 - accepted.** `Resolve-Path` throws a cryptic error when `-RepositoryPath` is missing.
  - ID: 15.1-RV15
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tools/release-preflight.ps1
  - Re-open trigger: A caller invokes the script with a stale or wrong `-RepositoryPath` and gets a generic `Cannot find path` error instead of an actionable message.
  - Rationale: Pre-check with `Test-Path -PathType Container` and throw a script-owned message. Minor UX improvement. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release-preflight script robustness sweep. Owner: release maintainer.

- **15.1-RV16 - accepted.** `GetReleaseWorkflowJobScalar` depends on 4-space indentation.
  - ID: 15.1-RV16
  - Status: accepted
  - Source story: 15-1-release-edge-case-preflight-hardening
  - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
  - Re-open trigger: A future `release.yml` reformat (2-space, tabs) silently passes the job-scalar contract test without actually inspecting the right scope.
  - Rationale: Structural YAML parsing would be ideal, but a hand-rolled prefix parser is consistent with the rest of `CiTestInventoryTests`. A broader test-helper sweep is the natural home. Story 19.3 (2026-06-30) buckets this as accept-until-trigger (no current failure or trigger; the fix is a release-owner decision); natural future home: release test-helper hardening sweep. Owner: release maintainer.

## Closed by: Deferred Work 13.7-RV4 Repository Root Locator Consolidation (2026-05-12)

- **13.7-RV4 — resolved.** The AppHost and Aspire integration fixture now share the
  AppHost-owned `RepositoryRootLocator` helper instead of maintaining duplicate
  `ResolveRepositoryRoot` implementations. The helper walks upward from the
  current directory and `AppContext.BaseDirectory`, fails closed when
  `Hexalith.Memories.slnx` is not found, and has focused unit coverage for both
  nested-directory discovery and missing-marker failure.

  - ID: 13.7-RV4
  - Status: resolved
  - Source story: 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide
  - Target artifact: src/Hexalith.Memories.AppHost/RepositoryRootLocator.cs
  - Re-open trigger: a third repository-root locator is introduced outside `RepositoryRootLocator`, or either AppHost startup or Aspire integration tests drift to a different root-discovery contract.
  - Evidence: Deferred-work implementation on 2026-05-12 added `RepositoryRootLocator`, replaced the AppHost and fixture helper copies with calls to it, and added `RepositoryRootLocator_NestedCurrentDirectory_ReturnsMarkerDirectory` plus `RepositoryRootLocator_MissingMarker_Throws`.

## Closed by: Story 14.5 Deferred Register Governance and Sprint-Status Hygiene (2026-05-04)

Story 14.5 introduces the structured field schema documented above and migrates
the four explicitly targeted register entries onto it. The migration is scoped to
the four IDs below plus the small fixtures the parser tests require; historical
prose entries are intentionally left untouched.

- **12.4-RV6 — resolved.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
  no longer classifies entries by substring scans for `baseline`, `test-release.ps1`,
  or `release lane`. The new `ParseStructuredDeferredEntries` reader matches anchored
  field labels (`ID:`, `Status:`, `Source story:`, `Target artifact:`, `Re-open
  trigger:`, `Evidence:` / `Rationale:`, optional `Test:`), and `ReadOpenDeferredBaselines`
  reports only entries whose `Target artifact` references the release-lane script
  with `Status: open`.

  - ID: 12.4-RV6
  - Status: resolved
  - Source story: 12-4-baseline-failures-sweep
  - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
  - Re-open trigger: any future change that reintroduces prose-substring classification of baseline-related deferred entries, or a parser regression where unrelated narrative mentions of `baseline` / `release lane` are once again counted as baseline filters.
  - Evidence: Story 14.5 replaced the substring-driven `baselineRelated` / `HasReleaseFilter` classifier with field-aware parsing; new fixture tests prove that prose mentions of `baseline`, `release lane`, and `test-release.ps1` in non-structured entries do not trigger baseline classification.

- **12.4-RV19 — resolved.** The legacy `DeferredKeyRegex` (`S11-F[A-Z0-9]+\.` with a
  literal trailing period) is replaced by reading the structured `ID:` field
  verbatim. The new parser accepts any ID token that the schema admits and rejects
  near-matches such as `12x4-RV6` or `112.4-RV6` exactly because field equality is
  enforced after extraction.

  - ID: 12.4-RV19
  - Status: resolved
  - Source story: 12-4-baseline-failures-sweep
  - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
  - Re-open trigger: a future deferred-work format change that adds new ID shapes (lowercase, em-dash, alternate suffix punctuation) without exercising them in `CiTestInventoryTests` fixtures.
  - Evidence: Story 14.5 deleted `DeferredKeyRegex` and now resolves IDs from the structured `ID:` field. Fixture tests cover `12.4-RV6`, `S11-FX`, lowercase / mixed-case rejection, and exact-ID boundaries against `12x4-RV6` and `112.4-RV6`.

- **12.6-RV2 — resolved.** This entry explicitly realized 12.4-RV6 and is closed
  by the same parser change. The unconditional `ShouldBeEmpty` assertion now
  rests on the structured-field reader rather than substring heuristics, so a
  prose-only edit to an unrelated entry (for example renaming "release pipeline"
  to "release lane") cannot flip an entry's classification.

  - ID: 12.6-RV2
  - Status: resolved
  - Source story: 12-6-embedding-input-content-kind-baseline-resolution
  - Target artifact: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs
  - Re-open trigger: a parser regression that reintroduces substring-based baseline classification, or a future story that adds a new release-lane filter without a paired structured deferred-work entry.
  - Evidence: closed alongside 12.4-RV6 by the Story 14.5 structured-field parser; new fixture test `ReadOpenDeferredBaselines_NarrativeMentionsBaseline_NotMisclassified` proves prose mentions are no longer load-bearing for classification.

- **13.7-RV5 — resolved.** Sprint-status history hygiene is now a documented
  forward-looking convention in `CONTRIBUTING.md`. Future status entries should
  use short dated breadcrumbs that link to the relevant story artifact, deferred
  entry, run log, or review document instead of accumulating multi-sentence
  evidence on a single YAML line. Historical Epic 1-13 history comments are
  intentionally not rewritten — that cleanup remains out of scope per the Story
  14.5 dev notes.

  - ID: 13.7-RV5
  - Status: resolved
  - Source story: 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide
  - Target artifact: CONTRIBUTING.md
  - Re-open trigger: a future parser, dashboard, or auditor that fails on the long historical YAML lines and proves a targeted edit to specific entries is required, or a contributor-process change that takes ownership of bulk sprint-status history rewriting.
  - Evidence: Story 14.5 added the "Sprint Status History Conventions" section to `CONTRIBUTING.md`; the Epic 14 bookkeeping rules in the same file require future Epic 14 stories to point at story artifacts and deferred IDs rather than appending narratives on the YAML status line.

## Closed by: Story 14.4 Migration and Integration Test Hardening (2026-05-04)

- **13.6-RV4 — closed.** `EmbeddingMigrationRedactor` now masks AWS long-term/temporary access key IDs (`A[KS]IA` + 16 alphanumeric, word-boundary anchored), raw JWT-shape tokens (`eyJ...` triplet) without a `Bearer` prefix, and HTTP Basic authorization values (`Basic <base64≥8>`) in addition to existing Bearer/Google/`client_secret`/JSON-escaped redactions. Boundary-spanning truncation guard preserved (redact-then-truncate-then-redact). New theory + fact tests cover AKIA/ASIA, raw JWT, Basic auth, JSON-escaped secrets, the existing happy path, and the truncation-boundary scenario.
- **13.6-RV5 — closed.** Verified preservation of name-only secret references (`client_secret named memories-embedding-client-secret`, `ApiSecretKeyName memories-embedding-client-secret`, `the secret 'memories-embedding-client-secret' could not be resolved`) via a new `[Theory]` test that asserts `[redacted]` does NOT appear and the benign secret-name remains operator-visible. The existing key=value redaction continues to mask actual secret values.
- **13.7-RV1 — closed.** `OllamaEmbeddingEndToEndTests.WaitForSemanticHashAsync` no longer enumerates the broad `{tenantId}:vec:*` pattern at default page size. Workflow status is parsed for `serializedOutput.memoryUnitId` and used for a targeted `HGET` against `{tenantId}:vec:{memoryUnitId}` whenever available. When the workflow has not yet produced a result, polling falls back to bounded SCAN with explicit `pageSize: 64` (SE.Redis maps `IServer.Keys` to SCAN under the hood for Redis 2.8+), wrapped in a linked `CancellationTokenSource` so the inter-poll `Task.Delay` is cancellation-aware. The timeout-diagnostic enumeration is also bounded (page size 64, top-50 keys only).
- **13.7-RV2 — closed.** Search query interpolation in `OllamaEmbeddingEndToEndTests` now uses `Uri.EscapeDataString` on `tenantId` and `canary` so future generator changes that introduce reserved URL characters cannot poison the request. Existing assertions unchanged.
- **13.7-RV3 — closed.** `AspireIngestionPipelineFixture.DeleteTempDaprConfig` now removes the fixture-owned `%TEMP%/hexalith-memories-dapr/{daprAppId}` directory in addition to `config.yaml`, including any AppHost-generated component yamls. Cleanup logic extracted into `internal static AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath, fixtureAppId)` with defense-in-depth: the leaf directory name must equal `fixtureAppId` before recursive deletion. The shared `%TEMP%/hexalith-memories-dapr` parent is never deleted. New Tier-2 tests in `OllamaOidcFakeServerTests` cover normal dispose, init-failure (file never written), defense-in-depth refusal on leaf-name mismatch, and null-config no-op.
- **13.7-RV6 — closed.** `OllamaOidcFakeServerTests` now contains an `[Theory]` with eleven `TokenRejectionScenario` cases covering missing `Content-Type` (text/plain body), missing `grant_type`, missing `client_id`, missing `client_secret`, duplicate values for each form field, wrong grant type, wrong scope, malformed body, and wrong HTTP method. Each case asserts `400 BadRequest`, `TokenRequestCount == 0`, `EmbedRequestCount == 0`, and `RequestEvidence` empty so a future regression that falls through to `AddEvidence`/`Increment` is caught immediately.
- **13.7-RV7 — closed.** The magic `ShouldBeGreaterThanOrEqualTo(2)` and `(1)` thresholds in `OllamaEmbeddingEndToEndTests` are replaced with named constants `MinimumRawAndNaturalLanguageEmbeddings` (= 2, raw + NL embed-call floor) and `MinimumTokenRequests` (= 1, with a comment explaining cached tokens may collapse multiple ingestions). A future refactor that legitimately changes either floor must update the named constant, making the rationale explicit.

### Carried forward by Story 14.4 (2026-05-04)

- **13.6-RV1 — carried forward.** Story 14.4 did not add ingestion-vs-migration coordination (out of scope per Dev Notes "Out of scope unless explicitly approved"). Story 13.7 integration evidence ran the migration tool to convergence without reproducing a mixed-provider tenant in the deterministic fake-Ollama path, but the production race window between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync` remains structurally present. Re-open trigger sharpened: any production migration where post-completion inventory shows a mixed-provider tenant, or any future story that introduces ingestion-vs-migration locking semantics.
- **13.6-RV3 — carried forward.** `EmbeddingVectorMigrationService` retains string-shaped error returns from `ValidateOptions` and `TryBuildTargetConfig` and routes all operator-visible failures through the structured `EmbeddingMigrationResult` surface. Adopting Hexalith's `ValueOrError<T>` convention requires a project reference to `Hexalith.Commons` (`src/libraries/Hexalith.Commons/Errors/ValueOrError{T}.cs` + `ApplicationError.cs`), which is in this story's forbidden-by-default file scope and would cascade through `Hexalith.Memories.Server`'s reference graph. The internal helpers feed exactly one consumer (the orchestrator) which immediately wraps each message into the public `EmbeddingMigrationResult`, so the local string shape is structurally equivalent to a `ValueOrError<T>` for this surface. Re-open trigger: a Hexalith-wide audit of result-pattern adoption that drops the `Hexalith.Commons` cross-project boundary, or any feature that needs to surface migration errors with `ApplicationError`'s richer shape (Title/Detail/TechnicalDetail/Arguments/Category) rather than a flat operator sentence.
- **13.7-RV4 — resolved 2026-05-12.** Story 14.4 did not introduce a new shared helper or touch a third copy of `ResolveRepositoryRoot`, so it carried the item forward. The later deferred-work implementation added the AppHost-owned `RepositoryRootLocator` and replaced both local helper copies with calls to it.

The Story 14.4 scope explicitly excludes `13.7-RV5` (sprint-status long-line cleanup); Story 14.5 owns sprint-status hygiene.

## Closed by: Story 14.3 OIDC and Embedding Security Hardening (2026-05-04)

- **13.2-RV1 — closed.** `OidcTokenProvider.GetOrFetchAsync` no longer flows the caller's `CancellationToken` into `_httpClient.SendAsync`. The fetch runs detached on the leader; per-caller cancellation flows through `Task.WaitAsync(ct)` at the public surface. New test `GetAccessTokenAsync_CancelledLeader_DoesNotPoisonSharedAcquisition` proves the leader's cancellation does not cancel the shared HTTP fetch and a same-key waiter still receives the original token without a second HTTP request.
- **13.2-RV2 — closed.** `OidcTokenProvider` now takes `IHttpClientFactory` and resolves a fresh `HttpClient` per fetch via `factory.CreateClient(HttpClientName)`, so handler rotation (DNS, TLS session pooling, `PooledConnectionLifetime`) is honored. `Program.cs` registration updated; `tools/MigrateEmbeddingVectors/Program.cs` updated as a Scope-Override to keep the standalone tool building (`SimpleHttpClientFactory` already implements `IHttpClientFactory`).
- **13.2-RV3 — closed.** `FetchTokenAsync` wraps `HttpRequestException`, `TaskCanceledException` (timeout, since the fetch is detached so any TCE here is a Timeout), and `IOException` in `OidcTokenAcquisitionException` with a sanitized correlation id, endpoint, and client id. New tests cover the three paths.
- **13.2-RV5 — closed.** `ValidateAndCreateKey` rejects token endpoints with non-empty `Uri.UserInfo`, query strings, and fragments. Error text deliberately does not echo any embedded credential value. New tests cover userinfo, query, and fragment rejection.
- **13.2-RV6 — closed.** Concurrent `InvalidateAndRefreshAsync` callers for the same key now collapse to a single in-flight fetch via the shared `_inflight` ConcurrentDictionary used by both regular and forced-refresh paths. New test `InvalidateAndRefreshAsync_ConcurrentForcedCallers_CollapseToOneRequest` proves the cap.
- **13.3-RV6 — closed.** Removed the optional default value from the 5-argument `EmbeddingClient` constructor; the 4-arg overload remains for tests/DI without `IOidcTokenProvider`, and the 5-arg overload requires explicit specification so DI ambiguity is closed.
- **13.3-RV7 — closed.** `EmbeddingClient.RedactSensitiveValues` now filters null/blank, applies `RedactionMinLength = 8`, deduplicates, and orders by descending length. New tests cover overlapping secrets and short benign substrings.
- **13.3-RV11 — closed.** `HandleEmbeddingResponseAsync` replaced `params string?[]` with `IReadOnlyCollection<string?> sensitiveValues`, moved the `CancellationToken` to the last parameter, and call-sites pass explicit collection-expression literals so accidentally added arguments cannot silently become redaction values.
- **13.3-RV12 — closed.** `EmbeddingClient` now calls `EnsureNonBlankBearerToken(...)` before constructing `AuthenticationHeaderValue("Bearer", token)` for both the initial token and the refreshed token. Whitespace tokens fail with a sanitized `EmbeddingApiException`. Theory test covers null/empty/whitespace.
- **13.3-RV14 — closed.** `EmbeddingClient.GenerateOllamaAsync` now wraps `OidcTokenAcquisitionException`, `HttpRequestException`, `IOException`, and `TaskCanceledException` (timeout) from both `GetAccessTokenAsync` and `InvalidateAndRefreshAsync` in `EmbeddingApiException` with the original exception preserved as `InnerException`. Caller cancellation is preserved as `OperationCanceledException`. New tests cover token acquisition exception wrapping and the Ollama transport-failure wrapping.
- **13.3-RV15 — closed.** `EmbeddingClient.GenerateOllamaAsync` evicts `_apiKeyCache.TryRemove(config.ApiSecretKeyName, out _)` before the 401/403 retry, then re-fetches the DAPR `client_secret` and uses the rotated value when calling `tokenProvider.InvalidateAndRefreshAsync(...)`. New test `GenerateAsync_Ollama_Unauthorized_EvictsApiKeyCacheBeforeRefresh` proves the rotated value reaches the token provider.
- **13.4-RV5 — closed.** `EmbeddingProviderDefaults.ValidateOptionalHttpUrl` rejects URLs with embedded user-info, query strings, and fragments for both `BaseUrl` and `OidcTokenEndpoint`. Error text does not echo any embedded credential or query value. New tests cover the three shapes per field.

## Deferred from: code review of story-13.6 (2026-05-03)

- **13.6-RV1 — Concurrent ingestion racing the migration.** `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — Between `SetEmbeddingConfigAsync` and `EnumerateSyntacticUnitsAsync`, a separate ingestion workflow with cached old config can write a fresh hash with the old provider/model; that unit is not picked up by enumeration and ends up in a mixed-vector tenant. Out of scope: ingestion-vs-migration coordination is broader than this tool. Re-open trigger: Story 13.7 integration suite, or any production migration that produces a mixed-provider tenant after run completion.
- **13.6-RV2 — Pre-existing missing copyright header.** `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs` — File lacks the standard `// <copyright file="..." company="ITANEO">` block. Pre-existing in HEAD; story 13.6 only added 3 lines for resume metadata stamping. Re-open trigger: any future story that touches the file substantively.
- **13.6-RV3 — Migration tool surfaces use ad-hoc string error returns + exit codes rather than `ValueOrError<T>`.** `src/Hexalith.Memories.Server/Migration/EmbeddingVectorMigrationService.cs` — `ValidateOptions` returns `string?`; result construction is manual. Hexalith convention prefers `ValueOrError<T>` for expected business failures. Re-open trigger: a refactor of the migration service surface, or a Hexalith-wide audit of result-pattern adoption.
- **13.6-RV4 — Redactor does not match AWS access keys, raw JWT signatures, or HTTP Basic auth.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — Embedding-provider error surfaces are unlikely to expose these credential shapes (Google API key + OIDC bearer + `client_secret` are the realistic vectors). Re-open trigger: a real exception payload caught in production that contains one of these shapes unredacted, or expansion to a third embedding provider.
- **13.6-RV5 — Redactor skips `client_secret named foo` style strings without `:` or `=` separator.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationRedactor.cs` — The regex correctly distinguishes secret-value from secret-name in the typical `key=value` shape; name-only references (e.g., "secret 'foo' not found") are not credential exposure. Re-open trigger: a CISO review or red-team finding flagging name-only references as exposure-equivalent.

## Deferred from: code review of story-13.5 (2026-05-02)

- **13.5-RV1 — `Hexalith.EventStore` submodule pointer bump bundled in feat commit.** Commit `8afea97` ("feat: Enhance TenantConfigurationActor and related tests for Ollama OIDC support") moved `Hexalith.EventStore` from `f812bfb` → `f8e8f14`. The story's "Expected edited files" list (`13-5-...md:241-246`) does not include `Hexalith.EventStore`, and project memory `feedback_submodule_init.md` plus `Hexalith.Commons/_bmad-output/project-context.md:99` explicitly warn against modifying Hexalith submodule pointers without explicit approval. Drift content verified innocuous (5 doc/story-tracking commits authored by Jerome — `f8e8f14`, `3bb39b8`, `56ccc45`, `e76adff`, `68b6957` — none touch the EventStore .NET binary surface). Accepted in-place; reverting now would just create churn. **Process note:** future feat commits should isolate ecosystem submodule bumps into a separate `chore: update subproject commit reference for Hexalith.EventStore` commit. Re-open trigger: any future feat commit that bundles a submodule pointer change.
- **13.5-RV2 — AC6 PUT/Conflict body not pinned end-to-end through ASP.NET Core's `HttpJsonOptions` pipeline.** `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs:69-130` — all new tests serialize via `MemoriesJsonContext.Options` directly; production `Program.cs` uses `Results.Ok(updatedConfig)` and `Results.Conflict(body)` which serialize through `IHttpJsonOptions`. If runtime HTTP JSON options ever diverge (different naming policy or converters), tests stay green while real bodies change. Re-open trigger: Story 13.7 integration suite landing is the natural enforcement point.
- **13.5-RV3 — No Ollama-flavored Provider/Model/Dimensions breaking-change actor tests.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:91-119` — existing Model/Dimensions breaking-change coverage uses `EmbeddingProviderDefaults.Google()` only; Ollama-specific `Validate(...)` ceilings (qwen3 dim lock at 2560, rate-limit ceiling 60_000) are exercised in `EmbeddingProviderDefaultsTests` separately. Re-open trigger: a second Ollama model lands and the dim/provider breaking-change matrix grows.
- **13.5-RV4 — Legacy `provider="ollama"` payload with missing OIDC fields not exercised.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:480-496` (`DeserializeLegacyGoogleConfig`) — pre-13.4 actor state cannot legitimately be Ollama because the provider was added in Story 13.1, but a hypothetical injected legacy Ollama payload's deserialize-then-Validate fallback path is un-pinned. Re-open trigger: any operational incident where an actor state predates the current provider list.
- **13.5-RV5 — Whitespace-only / empty-string `BaseUrl` legacy state behavior not pinned.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:197-219` — `ValidateOptionalHttpUrl` early-returns on whitespace for non-Ollama providers, so an empty/whitespace `BaseUrl` persists into `TenantConfigurationView`; for Ollama, validation rejects and the read path falls back to Google defaults. Low likelihood, low impact. Re-open trigger: a tenant config audit that surfaces an empty/whitespace `BaseUrl` in the wild.
- **13.5-RV6 — `FirstOllamaWrite_ShouldIgnoreClientSuppliedReindexFlag` does not isolate the two signals.** `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs:361-381` — passes both `forceReindex: true` and `newConfig.ReindexRequired = true`, so a regression respecting only one signal while ignoring the other would still pass. Mirrors the pre-existing Google `FirstWrite_ShouldIgnoreClientSuppliedReindexFlag` pattern (line 343); not a 13.5-introduced regression. Re-open trigger: a refactor of `TenantConfigurationActor`'s first-write semantics where the two signals are split into distinct branches.

## Deferred from: code review of story-13.3 (2026-05-02)

- **13.3-RV6 [resolved in 14.3] — Two public constructors create DI ambiguity surface.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:39-46,54-70` — 4-arg ctor delegates to 5-arg with `null`; 5-arg also has `IOidcTokenProvider? = null` default. MS DI does not honor C# default values, so the 4-arg overload is currently necessary. Remove the redundant default on the 5-arg side at next refactor. Re-open trigger: Story 13.7 wires `IOidcTokenProvider` into DI and the constructor surface is touched again.
- **13.3-RV7 [resolved in 14.3] — `RedactSensitiveValues` substring replace can over- or under-redact.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:483-495` — Short tokens or short input text could mask coincidental substrings of the upstream JSON; longer tokens with overlapping substrings get clobbered. Apply a length floor and order-by-length-descending replacement. Re-open trigger: a real-world incident where a redacted exception body becomes unreadable, or a security review.
- **13.3-RV8 — Asymmetric provider/model casing in parser output.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:139-146` — Provider lowercased, model preserved verbatim. May be intentional (Ollama tags can be case-sensitive). Re-open trigger: Story 13.4 / 13.5 introduces a persisted-config consumer that needs round-trip equality.
- **13.3-RV9 — No per-tenant circuit-breaker on persistent OIDC 401s.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:213-234` — AC5 mandates "exactly once" per request. Across many requests with a misconfigured client, each call still hits the IdP. Re-open trigger: a production incident where Keycloak traffic spikes correlate with embedding 401 storms.
- **13.3-RV10 — No 429/Retry-After test on the Ollama path.** `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs` — 429 mapping is provider-agnostic and reused for Ollama, but no test exercises it via the Ollama dispatch. Re-open trigger: Story 13.7 production hardening pass or a real Ollama gateway 429 incident.
- **13.3-RV11 [resolved in 14.3] — `params string?[]` after `CancellationToken` in `HandleEmbeddingResponseAsync`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:291-297` — Accidentally-added positional argument silently becomes a "sensitive value". Replace `params` with an explicit `IReadOnlyList<string?>` for security-critical parameters. Re-open trigger: any new caller of `HandleEmbeddingResponseAsync`, or a new sensitive value to redact.
- **13.3-RV12 [resolved in 14.3] — Whitespace token would crash `AuthenticationHeaderValue`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:285` — Interface does not enforce non-blank token. Current `OidcTokenProvider` validates; future provider implementation could return whitespace and crash with `FormatException`. Re-open trigger: a third-party `IOidcTokenProvider` is added, or the interface is opened to non-Hexalith implementations.
- **13.3-RV13 — `BaseUrl` with query string or fragment silently dropped.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:250-261` — `Uri.TryCreate` accepts `https://host/?k=v#frag`; the relative `Uri` resolution drops both. Story 13.4 validation narrows the gap. Re-open trigger: a tenant config audit surfaces a query/fragment in the wild.
- **13.3-RV14 [resolved in 14.3] — `InvalidateAndRefreshAsync` exceptions not wrapped in `EmbeddingApiException`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:216-218` — `OidcTokenAcquisitionException`, `HttpRequestException`, `TaskCanceledException` leak past the EmbeddingClient boundary. Mirrors deferred 13.2-RV3. Re-open trigger: a 401-retry production incident where typed transport errors are needed for retry classification at higher layers.
- **13.3-RV15 [resolved in 14.3] — Stale `client_secret` on Ollama 401 retry.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs:201,217` — If the DAPR `client_secret` is rotated, cached secret stays in `_apiKeyCache`; bearer-token refresh hands the IdP the stale secret. Google path evicts the secret cache symmetrically (line 176); Ollama does not. AC5 does not strictly require this. Re-open trigger: a secret-rotation runbook where Ollama tenants degrade until restart.

## Deferred from: code review of story-13.4 (2026-05-02)

- **13.4-RV1 — `RateLimitPerMinute` boundary / arithmetic overflow concerns.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:147-161` — Validator caps the value but downstream arithmetic on it is not audited. Pre-existing. Re-open trigger: any throughput refactor that multiplies rate by a window size or uses it in token-bucket math.
- **13.4-RV2 — `OidcScope` whitespace-only not validated.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — Spec leaves `OidcScope` optional and unvalidated; a non-null whitespace-only value would silently flow into the token request and surface as `invalid_scope` from Keycloak. Out of story scope. Re-open trigger: Story 13.2 / 13.3 surfaces an IdP-side regression caused by malformed scope.
- **13.4-RV3 — OIDC mode does not enforce `ApiSecretKeyName` distinctness/role.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:163-181` — A tenant migrating from Google to OIDC Ollama could carry over a Google API-key secret name (`google-embedding-api-key`); validator only enforces the regex shape. Operator footgun. Re-open trigger: Story 13.5 surface change that exposes a tenant-config diff/mutation endpoint where naive carry-over is plausible.
- **13.4-RV4 — No assertion that endpoint paths invoke `Validate`.** `src/Hexalith.Memories.Server/Endpoints/*` — Validator hardening (auth modes, URL shape, OIDC requirements) is dead code if no caller invokes it on POST/PUT. The single endpoint test in this story asserts JSON projection on a hand-built `TenantConfigurationView`, not the full ingest path. Cross-cutting concern. Re-open trigger: Story 13.5 (`TenantConfigurationActor` storage flow) or Story 13.7 (integration tests) — at least one should pin the actor/endpoint→Validate contract.
- **13.4-RV5 [resolved in 14.3] — URLs with userinfo (`https://user:pw@host`) accepted by `ValidateRequiredHttpUrl`.** `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:214-226` — Mirrors deferred 13.2-RV5 in the OIDC token provider. `Uri.TryCreate` accepts userinfo and the value is preserved; backend-configured endpoints make tenant exploitation rare, but defensive rejection is cheap and uniform across providers. Re-open trigger: any tenant config audit that finds embedded credentials, or a security review that wants the rule applied uniformly across `OidcTokenProvider` + `EmbeddingProviderDefaults`.

## Deferred from: code review of story-13.2 (2026-05-02)

- **13.2-RV1 [resolved in 14.3] — Leader cancellation poisons shared HTTP fetch.** `OidcTokenProvider.cs:117,167` — the leader's `CancellationToken` is passed straight into `_httpClient.SendAsync`. AC6 narrow text only requires waiter cancellation isolation (current test `GetAccessTokenAsync_CancelledWaiter_DoesNotCancelSharedAcquisition` covers this). Dev Notes Implementation Guidance is stricter: "a single caller cancellation must cancel that caller's wait without cancelling the in-flight fetch for remaining waiters." If the leader cancels mid-fetch, queued waiters re-enter and refire. Fix requires TCS-based detached-fetch refactor or linked-CTS where the inner SendAsync uses `CancellationToken.None` and waiters await via `Task.WaitAsync(ct)`. Re-open trigger: Story 13.3 retry integration where leader-cancel under 401 retry becomes concrete; or a production incident where IdP traffic spikes correlate with caller cancellations.
- **13.2-RV2 [resolved in 14.3] — Singleton-captured HttpClient bypasses `IHttpClientFactory` handler rotation.** `Program.cs:110-118` and `OidcTokenProvider.cs:34-42` — the named HttpClient is resolved once at singleton activation and stored for the service lifetime. DNS changes, TLS session rotation, and `SocketsHttpHandler.PooledConnectionLifetime` rotation never apply. The same caveat exists for `EmbeddingClient` registration (line 108). Fix options: inject `IHttpClientFactory` and `CreateClient(name)` per call, or convert to typed-HttpClient + scoped lifetime. Re-open trigger: an ops incident traceable to stale TLS/DNS, or an ecosystem-wide pass to standardize HttpClient lifecycle across `EmbeddingClient` + `OidcTokenProvider`.
- **13.2-RV3 [resolved in 14.3] — Network/timeout exceptions not wrapped in `OidcTokenAcquisitionException`.** `OidcTokenProvider.cs:167` — `HttpRequestException`, `TaskCanceledException` (timeout), `IOException` from `SendAsync` propagate raw. AC7 only requires wrapping non-2xx responses, but Story 13.3's 401-retry will distinguish recoverable vs terminal failures. Re-open trigger: Story 13.3 surfaces a need for typed transport errors during retry classification.
- **13.2-RV4 [resolved in 15.4] — `http://` token endpoint scheme accepted (no TLS enforcement).** `OidcTokenProvider.cs:80` — `uri.Scheme is not "https" and not "http"` is the only scheme guard. Dev/local Keycloak needs `http://localhost`; production must be constrained at the operations/config layer. Re-open trigger: Story 13.7 operations docs / production hardening pass.
- **13.2-RV5 [resolved in 14.3] — Token endpoint with userinfo (`https://user:pw@host`) accepted.** `OidcTokenProvider.cs:79-89` — `Uri.TryCreate` accepts userinfo and it is preserved through `UriComponents.SchemeAndServer`. Backend-configured endpoints make this rare; defensive rejection is still cheap. Re-open trigger: any tenant config audit that finds embedded userinfo, or a security review.
- **13.2-RV6 [resolved in 14.3] — Concurrent `InvalidateAndRefreshAsync` callers can each fire a fetch.** `OidcTokenProvider.cs:65,116-119` — two concurrent forced-refresh callers both skip the cache double-check inside the guard and each issue a fresh HTTP fetch. AC5 is silent on concurrent forced refresh; AC6's "exactly one outbound HTTP request" applies to cache-miss collapse, not invalidation. Re-open trigger: a 401 storm during ingestion that hammers Keycloak via simultaneous retry-after-401 paths in Story 13.3.
- **13.2-RV7 — Unbounded `_cache` and `_guards` growth + undisposed `SemaphoreSlim`.** `OidcTokenProvider.cs:24-25` — singleton dictionary growth is bounded by unique `(endpoint, clientId, scope)` tuples but never evicted; semaphores in `_guards` are never `Dispose()`'d. Re-open trigger: a long-running tenant churn scenario or a leak diagnostic that traces growth to this provider.
- **13.2-RV8 — `JsonDocument.Parse` materializes adversarial large bodies; `InvalidOperationException.Message` may pass provider-controlled text.** `OidcTokenProvider.cs:188,217` — a 200 OK with an outsized body is read fully before parsing; provider-supplied `tokenType`/`propertyName` values appear inside the malformed-response reason string (then in the typed exception message). Modern HttpClient defaults bound the buffer at 2 MiB so practical risk is low. Re-open trigger: an SLA pass that wants explicit `MaxResponseContentBufferSize` or a sanitization audit on exception text.
- **13.2-RV9 — `ScriptedTokenHandler.Requests` is a non-thread-safe `List<T>` mutated from concurrent `SendAsync`; `WaitForRequestsAsync` TCS is single-shot.** `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs:404,406-412` — concurrency tests fire two parallel handler invocations; `List.Add` resizing under contention can throw or produce wrong `Count`. Tests pass on current schedulers. Re-open trigger: first observed flake in `GetAccessTokenAsync_ConcurrentDifferentKeys_DoNotBlockEachOther` or `GetAccessTokenAsync_ConcurrentSameKey_SendsSingleRequest`.

## Deferred from: code review of story-13.1 (2026-05-02)

- **13.1-RV1 — `Validate_GoogleAtRateLimitAboveOllamaCeiling_ShouldThrow` test name vs body.** Test uses `RateLimitPerMinute=5000`, ABOVE Google's 3000 ceiling but BELOW Ollama's 60_000 ceiling. Name is internally inconsistent with the value — test correctly verifies per-provider partitioning; the name is misleading. Spec-mandated; rename should accompany next provider addition. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:266-272`)
- **13.1-RV2 — `Validate_OllamaQwen3_AcceptsExactly2560` named "accepts" but asserts "rejects".** Every `[InlineData]` value (2559, 2561, 768, 1024, 1536) expects throw; no positive case covers 2560 except via the default factory. Spec-mandated name (Subtask 3.12). Cleanup: rename to `Validate_OllamaQwen3_RejectsAnyDimensionExcept2560` plus explicit `[InlineData(2560)] => ShouldNotThrow` companion. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:296-307`)
- **13.1-RV3 — `Validate_OllamaProviderWithGoogleModel_DimensionMismatch_ShouldThrow` body uses Ollama model, not Google.** Test name says "GoogleModel"; body uses `Model="qwen3-embedding:4b"` (Ollama). Dev followed spec body verbatim — spec body itself is internally inconsistent with the test name. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs:274-284`)
- **13.1-RV4 — Provider whitespace UX gap.** `Validate(... with { Provider = " ollama" })` throws `Provider ' ollama' is not supported. Supported providers: 'google', 'ollama'.` — technically correct but obscures the leading-whitespace root cause. No security risk. Trim before comparing or surface a "leading/trailing whitespace?" hint. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)
- **13.1-RV5 — Per-provider rate-limit ternary fragile for future providers.** `int maxRateLimit = provider == ollama ? 60_000 : 3_000` silently uses Google's ceiling for any unknown provider added through `IsSupportedProvider`. Refactor to `IDictionary<string,int>` ceiling lookup at the same pass that introduces the per-model dim registry (Round 1 finding §2 / spec "When a third Ollama model is added"). (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:140-145`)
- **13.1-RV6 — `Dimensions = int.MaxValue` accepted.** Pre-existing — only the `<=0` lower-bound is checked. A 2.1B-dim vector would 404 at the index store rather than failing at config-time. Cap at a shared upper bound (e.g., 16_384) when the embedding registry refactor lands. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117`)
- **13.1-RV7 — `GetBreakingChangeFields` case-sensitivity contract not pinned by tests.** Pre-13.1 `GetBreakingChangeFields` uses `OrdinalIgnoreCase` for Provider/Model — a regression flipping to ordinal would silently report a casing-only delta as a breaking change. Pre-existing test gap. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-67`)
- **13.1-RV8 — No null-config test for `Validate(null!)`.** `ArgumentNullException.ThrowIfNull(config)` is at the top of `Validate` but no test pins the contract. Pre-existing pattern across the suite. (`tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`)
- **13.1-RV9 — Default Ollama RateLimit (6000) vs ceiling (60_000) divergence undocumented at call-site.** Spec rationale exists but the constant doc on `OllamaMaxRateLimitPerMinute` only documents the ceiling. Add an inline XML comment at `Ollama()`'s `RateLimitPerMinute = 6000` line when 13.5 wires the actor surface. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:50-57`)
- **13.1-RV10 — Mixed-case provider/model strings persisted verbatim.** `OrdinalIgnoreCase` matching but no normalization of stored values. A tenant config persisting `Provider="Ollama"` survives validation; a downstream comparator using ordinal equality (e.g., the `{provider}:{model}` parser owed by Story 13.3) would silently disagree. Story 13.3's `ParseProvider` contract test is the natural enforcement point. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:99-104`)
- **13.1-RV11 — Tolerant defaults in `Validate(...)`: cross-pollinated configs slip through (HIGH, owner Story 13.4).** The dim assertion is keyed on model name only (not provider+model), and `ModelNamePattern` (`^[A-Za-z0-9.:_-]+$`) requires no alphanumeric. Validator currently accepts: (a) `Provider="google", Model="qwen3-embedding:4b", Dimensions=2560`; (b) `Provider="ollama", Model="gemini-embedding-001", Dimensions=768`; (c) `Provider="ollama", Model="totally-fake", Dimensions=1`; (d) `Model=":::"` / `Model="-"`. **Action when 13.4 lands:** (1) introduce `provider→{model→dim-allowlist}` registry, (2) tighten regex to `^[A-Za-z0-9][A-Za-z0-9.:_-]*$`, (3) add cross-pollination negative tests. **Re-open trigger if 13.4 ships without:** any 13.2/13.3 test having to special-case a cross-provider config; any operational incident where config validates but embedding fails. Bundled per `feedback_tolerance_idioms.md`. (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:117-145, 167`)

## Deferred from: code review of story-12.6 (2026-05-02)

- **12.6-RV1 — Real-repo positive parser canary lost.** `ReadAcceptedReleaseFilters_RealRepo_HasNoAcceptedBaselineFilters` and `TestReleaseBaselineFilters_ShouldMatchOpenDeferredWorkEntries` both now expect empty against the real repo files; only fixture-shaped tests prove `ReadOpenDeferredBaselines` parses anything. Add a smoke test that exercises the parser against a fixture mirroring the current `deferred-work.md` (or a separate non-baseline open `S11-F*` entry) so future structural drift in the file format is caught loudly. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:217-224`)
- **12.6-RV2 [resolved in 14.5] — `baselineRelated` heuristic backs the unconditional `ShouldBeEmpty` assertion.** `ParseDeferredBaseline` classifies an entry as "baseline-related" via case-insensitive substring of `baseline` or `test-release.ps1`. A pure-prose deferred-work edit (e.g., S11-FD "release pipeline" → "release lane") could flip an entry's classification and break the inventory test with no functional change. Migrate to a structured classifier (e.g., explicit `Filter:` field per entry) before the surface grows. Realizes the 12.4-RV6 concern. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:372-377`)
- **12.6-RV3 — Single-item parser fixture masks over-matching.** `ReadAcceptedReleaseFilters_ValidKeyedFilter_ReturnsFilter` exercises exactly one comment + one filter line and uses `ShouldHaveSingleItem()`, which would pass even if the parser is matching on the wrong line and dedupes. Strengthen with a 2-filter fixture or one with a comment-line that resembles a filter, to verify the proximity guard and uniqueness logic. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:198-214`)
- **12.6-RV4 — Discoverability breadcrumb removed from `tools/test-release.ps1`.** Along with the `$projectFilters` block went the only on-script reference to deferred-work bookkeeping. Consider a one-line trailing comment such as `# No per-project baseline waivers; if one becomes necessary, register it in _bmad-output/implementation-artifacts/deferred-work.md and pair it here.` so a future maintainer searching the script for "baseline" finds the policy. (`tools/test-release.ps1:25`)
- **12.6-RV5 [resolved 2026-05-19] — Underlying S11-FA test still used fixed tenant id `"t"` and a non-thread-safe capture list.** `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filtered captures by neither tenant nor instrument-source, while its sibling `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` used unique tenant id + `ShouldHaveSingleItem`. The flake mode that originally motivated S11-FA was dormant, not eliminated, and could re-trip under heavier xunit parallelism or any other test that emits on the static `MemoriesMeter.EmbeddingApiCalls` counter. Resolved by the 2026-05-19 deferred-work implementation: the affected telemetry tests now use unique tenant ids, tenant-filtered metric capture, `ConcurrentQueue`, and single-event assertions. Re-open trigger: any flake reappearance on `EmbeddingInputContentKindTests`, or before any future story that adds a third concurrent `EmbeddingApiCalls` test path. (`tests/Hexalith.Memories.Server.Tests/NaturalLanguage/EmbeddingInputContentKindTests.cs`)

## Deferred from: code review of story-12.5 (2026-05-02)

- **12.5-RV1 — Workflow hardcodes summary path.** `.github/workflows/release.yml:75` literal `artifacts/packages/release/publish-summary.json` is a duplicate of the path computed by `tools/publish-nuget.ps1` from its `-PackageDirectory` parameter. Aligned today via `.releaserc.json` invocation; if either ever changes the alert silently no-ops.
- **12.5-RV2 — `gh issue list --search` lacks `in:title` qualifier and uses default `--limit 30`.** `tools/create-partial-publish-issue.ps1:92`. Local `Where-Object { $_.title -eq $title }` defends against substring collisions today, but a high-volume backlog of partial-publish issues could push the exact match off the result page. Switch to `--search "in:title \"$title\""` and bump limit when revisited.
- **12.5-RV3 — Race on concurrent partial-publish runs.** Two workflow runs hitting partial-publish for the same version simultaneously can each create a new issue. `tools/create-partial-publish-issue.ps1:92-119`. Workflow `concurrency: release` reduces but does not eliminate the window. Needs server-side dedupe (search-with-create-or-comment idempotency) before becoming critical.
- **12.5-RV4 — `Format-ListSection` does not skip `$null` items.** `tools/create-partial-publish-issue.ps1:52-67`. Current JSON shape never emits nulls; a future contract change could produce blank `- :` bullets.
- **12.5-RV5 — No retry/backoff for transient `gh` failures in the alert path.** `tools/create-partial-publish-issue.ps1:92-128`. A flaky GitHub API turns partial-publish into "release failed AND alert step failed." Release failure is still loud; alert reliability is the deferred gap.
- **12.5-RV6 — Malformed `publish-summary.json` makes the alert step throw.** `tools/create-partial-publish-issue.ps1:32`. `ConvertFrom-Json` has no `try`; a half-written summary causes the alert step to fail with a JSON-parse trace and obscure the original publish failure.
- **12.5-RV7 — Empty stdout from `gh issue list` trips `ConvertFrom-Json`.** `tools/create-partial-publish-issue.ps1:97`. Add `if ([string]::IsNullOrWhiteSpace($issuesJson)) { $issues = @() }` guard.
- **12.5-RV8 — Closed-then-reopened partial-publish issue creates a duplicate.** `tools/create-partial-publish-issue.ps1:92` filters `--state open` only. After a maintainer manually reconciles and closes the issue, a same-version rerun creates a new issue rather than reopening or commenting. Spec is silent; semantics may need refinement after first real reconciliation cycle.

## Deferred from: code review of story-12.3 (2026-05-01)

- **12.3-RV1 — CI duplicate runs on `pull_request` and `push`.** Concurrency keys differ (`pull_request.number` vs `github.ref`) so neither cancels the other. Not regression-critical; revisit when CI minutes become a constraint or when divergent results from the two paths cause confusion. [.github/workflows/ci.yml:3-9]
- **12.3-RV2 — CI fork-PR base SHA reachability.** `actions/checkout@v6` with `fetch-depth: 0` on the head ref does not fetch the base repo's history. `git diff "$base_sha" "$head_sha"` may fail on PRs from forks. Deferred until fork PRs are accepted; if needed, add an explicit `git fetch origin "$base_sha"` step. [.github/workflows/ci.yml:43-46]
- **12.3-RV3 — CI brand-new branch first push enumerates entire head commit.** `git diff-tree --no-commit-id --name-only -r "$head_sha"` lists every file at HEAD when `before` is `0000…0`, producing a massive out-of-scope list. Pair with the force-push hardening patch P5; for now, the failure mode is loud and recoverable. [.github/workflows/ci.yml:51-52]
- **12.3-RV4 — Branch with no story key + no `Story:` trailer fails closed across all CI.** No allowlist for automation branches (dependabot, renovate). Defer until automation PRs are configured for this repo. [tools/check-story-file-scope.py:168-172]
- **12.3-RV5 — `commit-msg` re-reads index after pre-commit may have modified it.** File set seen by the two hooks can differ in environments that auto-format during pre-commit. Low impact in this repo; reconsider if formatters are introduced. [.githooks/commit-msg:12]
- **12.3-RV6 — `read_commit_message` raises `UnicodeDecodeError` on non-UTF-8 messages.** Edge case; clean error wrapping is a follow-up nicety. [tools/check-story-file-scope.py:114-118]
- **12.3-RV7 — `--changed-files-file` does not strip a UTF-8 BOM.** A PowerShell `Set-Content`-emitted file would silently mismatch the first path. Switch to `utf-8-sig` decoding when revisited. [tools/check-story-file-scope.py:246]
- **12.3-RV8 — `collect_changed_files` silently drops paths normalizing to empty.** Theoretical; `..`-only inputs are not produced by `git diff --name-only`. [tools/check-story-file-scope.py:250]
- **12.3-RV9 — Pre-commit fails closed during rebase / cherry-pick / detached-HEAD.** `git branch --show-current` returns empty; with no other story-key source the validator blocks. Needs UX design before patching. [.githooks/pre-commit:7]
- **12.3-RV10 — `python` fallback may land on Python 2 on legacy systems.** Not a target environment today. [.githooks/pre-commit:13-17]
- **12.3-RV11 — Hooks consume newline-separated `git diff --name-only` output.** Filenames containing newlines (legal POSIX) mishandled. No such filenames in repo. [.githooks/pre-commit:12]
- **12.3-RV12 — `is_vague` mixes raw `pattern` and post-normalized `normalized` for special-char check.** Backslashes get normalized away before the test. Pair with override-vagueness rework P6. [tools/check-story-file-scope.py:286-288]
- **12.3-RV13 — `parse_allowed_scope` does not honor `### ` subheadings inside `## File Scope`.** No current story uses this shape. [tools/check-story-file-scope.py:206-211]
- **12.3-RV14 — `ALLOWED_LABELS` set has aliases (`Expected files to add or edit:`, `Allowed to modify:`) not in CONTRIBUTING.md.** Either remove the aliases or document them in a follow-up. [tools/check-story-file-scope.py:19-23]
- **12.3-RV15 [resolved in 14.1] — Multiple `Allowed files for this story:` blocks in one story merge silently.** No current story uses this shape. [tools/check-story-file-scope.py:217-223]

## Closed by: course correction (2026-04-26)

- **W10 — closed.** AC #6 in Story 11.1 was marked `[x]` while branch protection remained pending maintainer action; `docs/dev/branch-protection.md` already documented the dependency but the task checkboxes alone were misleading. Course correction added an explicit `External Action Pending` status line at the top of `_bmad-output/implementation-artifacts/11-1-github-actions-build-and-test-pipeline.md` calling out the maintainer-only GitHub-settings step. AC #6 task checkboxes (3.4, 3.5, 4.4) remain `[x]` because they cover the documentation work, which is complete; the in-GitHub apply step lives outside the repository and is now visible at the top of the story file rather than buried in the AC text. P1 (`git add package-lock.json`) was resolved separately by commit `5eecf4c` which bundled `package-lock.json` with the rest of the 11.1 + 11.2 work.

## Closed by: Story 12.6 EmbeddingInputContentKind Baseline Resolution (2026-05-02)

- **S11-FA — closed.** `EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` passed under a clean Release test run, the full `EmbeddingInputContentKindTests` class passed, and the stronger `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` theory passed. The stale `tools/test-release.ps1` `FullyQualifiedName!~EmbeddingInputContentKindTests.ContentKind_PropagatesToEmbeddingApiCallsMetricTag` filter was removed, returning accepted Server.Tests release-lane baseline filters to zero.

## Closed by: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)

- **Story-10.2-TokenBudgetServerTruncation — closed.** MCP forwards `tokenBudget` to the server, server-side search/traverse truncation populates `omittedCount`, `estimatedTokensTotal`, and `omittedReason`, and the 10.1 client-side soft clamp was removed.
- **Story-10.2-DegradedStateAnnotations — closed for MCP ingress.** Search and traversal response contracts now expose degraded-state metadata, and the server populates single-axis/hybrid/traverse response envelopes where degradation can be detected.
- **Story-10.2-IngressAuthentication-NFR11 — closed for MCP ingress.** `/mcp` now uses JWT bearer auth, MCP auth metadata, SDK authorization filters, endpoint authorization, and per-tool tenant-claim checks; `McpUnauthenticatedStartupGuard` and its tests were deleted.

## Deferred from: Story 10.2 Token-Budget Responses & Authentication (2026-04-25)

- **Story-10.x-McpTraceHopAspire (closes AC #12).** `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs::TraceHop_McpToServer_PreservesTraceparent` — assert MCP→Memories Server parent/child trace span relationship at Tier-3. Implementation requires a parallel breadcrumb path through the MCP Aspire process (the existing `AspireEndToEndTraceTests` uses an in-process collector for the CLI side and audit-log JSON breadcrumbs for the Server side; the MCP service emits ActivitySource spans into its own process and lacks an in-test exporter). 10.2 ships the prerequisite Tier-3 auth bearer-minting fixture (`AspireIngestionPipelineFixture.MintDevBearer`) so the trace-hop work can attach to it directly. Re-open trigger: nightly Tier-3 lane stabilizes against the new auth-required surface and an in-test exporter for the MCP process becomes affordable; or first observation of MCP→Server trace breakage in production OTel data.
- **Story-10.x-McpAuthTier3ExtendedSuite.** Additional Tier-3 cases beyond the four critical scenarios shipped in 10.2 (`PostMcp_NoAuthorizationHeader_ReturnsBearerChallenge`, `GetHealth_AllowsAnonymous`, `CallTool_ValidBearer_MatchingTenantClaim_Succeeds`, `CallTool_ValidBearer_CrossTenantClaim_ReturnsTenantForbidden`): expired-bearer test, clock-skew tolerance test (M3), cross-request alternating-tenants leak test (P2), tool-class lifetime convention test (P1). Tier-2 already covers expiry / clock-skew at unit level (`ConfigureJwtBearerOptionsTests`); cross-request leak is structurally prevented by `TenantClaimAuthorizationFilter`'s `InvalidOperationException` guard (P3). Re-open trigger: first nightly Tier-3 regression against the auth path, or first cross-request leakage report.
- **Story-10.x-McpStatelessTripwireTest (W3).** `McpServerStatelessTransportGuardTests.Stateless_IsTrue_AndChangeRequiresAdrUpdate` — assert `WithHttpTransport(o => o.Stateless).Should().BeTrue()`. Requires an MCP-builder introspection surface that the SDK does not currently expose. Re-open trigger: SDK exposes a public reader for `WithHttpTransport` configuration, or any follow-up story discusses OAuth-PKCE / sampling / elicitation.
- **Story-10.x-McpAuthAnonymousDevBindAddress (A2).** Bind-address invariant for the anonymous-dev gate — refuse to run when `Authentication:JwtBearer:Authority` AND `SigningKey` are both unset and the process binds a non-loopback address. The 10.2 wiring requires `Authority` OR `SigningKey` at startup (validator hard-fails otherwise), so the anonymous-dev path no longer exists; this defense-in-depth check is N/A unless the anonymous-dev path is reintroduced. Re-open trigger: any future story re-introducing an anonymous mode for development.
- **Story-10.x-McpAuthDriftDetectorCi (A14).** Monthly GitHub Actions workflow that diffs `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/` against `src/Hexalith.Memories.Mcp/Authentication/` and posts structural drift summaries to an ops issue. Allowed-divergence list mirrors ADR-10.2-001 invariants. Effort > 45 min for a robust diff + allowlist + ops-issue integration; deferred to keep the 10.2 envelope on track. Re-open trigger: first observed drift incident, or quarterly review.
- **Story-10.x-RetroLessonForwardReferenceGuards (A16).** Capture the "10.1 `McpUnauthenticatedStartupGuard` lifecycle pattern" as a retro lesson (forward-reference guards have non-trivial N+1 deletion cost; account for it in N+1 effort estimates). Lands in the Story 10.2 retro deliverable.

## Deferred from: Story 10.1 MCP Server & Tool Registration (2026-04-25)
- **Story-10.x-McpTraceHopAssertion.** `tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs` does not yet include an MCP-hop assertion — adding one is out of scope for 10.1 and tempting-but-fragile until 10.2 auth is wired (so the test does not need to mint bearer tokens). The MCP integration test (`McpServerIntegrationTests.CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop`) covers a happy-path execution, but does NOT yet assert that the outbound trace contains a span resolving to the DAPR sidecar invocation path (`/v1.0/invoke/memories-server/method/*`). **Re-open trigger:** post-10.2 follow-up story or first observation that direct-HTTP regressions slip through.
- **Story-10.x-StatelessModeAuditFor10.2Auth — closed for bearer auth.** Story 10.2 kept `WithHttpTransport(o => o.Stateless = true)` because the implemented flow is bearer-only and validates each request independently. Re-open if OAuth-PKCE, refresh-token rotation, sampling, or elicitation requires server-side session state.
- **Story-10.x-McpAotCompatibility.** ModelContextProtocol 1.2.0 uses reflection for tool-schema generation. Setting `<PublishAot>true</PublishAot>` on `Hexalith.Memories.Mcp.csproj` will likely surface trim/reflection warnings. 10.1 deliberately leaves AOT off — same default as `Contracts`, `Client.Rest`, `EventStore`, `Cli`. **Re-open trigger:** an explicit AOT-publishing requirement, or upstream MCP SDK release that ships a source-generator-based schema path.
- **Story-10.x-McpTokenizerAccurateBudget.** Story 10.2 replaced the 10.1 per-result soft clamp with server-side `contentSnippet.Length / 4 + overhead` estimation. This is still heuristic and can over-prune non-ASCII content. Phase 2 Sprint 1 candidate; escalate on first non-ASCII tenant onboarding or quantitative observation that the estimate causes >2x under-utilization on real workloads.
- **Story-10.x-TraverseSemanticPrimaryPath.** Story 10.2 traversal truncation preserves a BFS shortest path to the deepest node before pruning branches. A semantically weighted primary path (`causedBy` > `correlatedWith`, etc.) may produce better narratives under tight budgets, but edge weights are not exposed today. Re-open when traversal edge weights become available.
- **Story-10.x-OpenTelemetryAspNetCoreAlignment.** Story 10.1 bumped `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.InMemory` from 1.15.1 → 1.15.3 to clear NU1902 advisories (GHSA-mr8r-92fq-pj8p, GHSA-q834-8qmm-v933). `OpenTelemetry.Instrumentation.AspNetCore` only has 1.15.2 published (no 1.15.3), so it stays at 1.15.2 — the advisories did not target it. **Re-open trigger:** AspNetCore instrumentation lands a 1.15.3 patch and we want to re-align all OTel pins on the same point release.

## Deferred from: Story 9.3 Handler Registration & Mismatch Detection (2026-04-24)

- **Story-9.3-MemoriesServerAuthN — resolved by Story 20.1.** Memories Server now registers JWT bearer authentication and fallback `RequireAuthenticatedUser` authorization, wires authentication/authorization middleware, and keeps only named infrastructure/Dapr exceptions anonymous (`/health`, `/alive`, `/ready`, `/dapr/subscribe`, `/events/ingest`, plus Dapr actor runtime handlers as a non-`/api` internal exception). Evidence: `src/Hexalith.Memories.Server/Authentication/*`, `src/Hexalith.Memories.Server/Program.cs`, `src/Hexalith.Memories.ServiceDefaults/Extensions.cs`, `src/Hexalith.Memories.EventStore/EventIngestionController.cs`, and `tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs`. Tenant membership authorization and principal-derived audit identity are resolved separately by Story 20.2.
- **D8 TenantAuthorizationMiddleware / A2 caller-asserted tenant identity — resolved by Story 20.2.** Memories Server now normalizes authenticated principal tenant claims into a server-owned `memories:tenant` claim, rejects well-formed cross-tenant `/api/tenants/{tenantId}/**`, `/api/search?tenantId=...`, and ingest scheduling requests before endpoint business logic/backend access, and derives audit user identity from the authenticated principal instead of `x-user-id` or request-body attribution fields. Evidence: `src/Hexalith.Memories.Server/Authentication/ServerTenantClaimsTransformation.cs`, `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationMiddleware.cs`, `src/Hexalith.Memories.Server/Authentication/TenantAuthorizationEndpointFilter.cs`, `src/Hexalith.Memories.Server/Program.cs`, `tests/Hexalith.Memories.Server.Tests/Authentication/*`, and `tests/Hexalith.Memories.Server.Tests/Telemetry/AuditLogStreamTests.cs`. Residual tenantless workflow/batch status scoping remains Story 20.3 scope, not a duplicate D8/A2 entry.
- **Story-9.3-ObservationWindowConfig.** The 24h observation window is hardcoded in `HandlerRegistryService.ObservationWindow`. Making it configurable per-tenant complicates Redis TTL (TTL must exceed the widest possible window) and global-config awkwardness. **Re-open trigger:** first operator explicitly requests a non-24h window in a real deployment.
- **Story-9.3-ProjectionRegistryCrossCheck.** The detector validates observed events against the ROUTING config (`SourceToTenantMap`), NOT against the set of projections the tenant's application code has bound at runtime. An event can be "handled from routing's POV" but silently ignored downstream by the application. A declarative projection registry (attribute-scanned, reflection-verified) is the right future solution. **Re-open trigger:** operator-driven demand for this gap to be closed.
- **Story-9.3-SinceFlagForLowVolume.** `--since <duration>` CLI flag on `memories handlers mismatches` to widen the observation window for low-volume tenants, reducing `StaleHandler` Info noise on weekly-publishing patterns. Requires the observation store TTL to be widened to `2 × max(window)` globally, or a dedicated expanded-window store. Material cost, deferred.
- **Story-9.3-TenantCardinalityBucketing.** Switch `memories.handlers.registered` from `tenant_id`-tagged gauge to a bucketed summary (0 / 1-10 / 10-100 / 100+ tenants) when N ≥ 1000 tenants approaches. Not an issue in current deployments. **Re-open trigger:** tenant count crosses 1000 in any real environment.
- **Story-9.3-VersionMismatchAttributeApproach.** Replace regex-based `VersionMismatch` with a publisher-declared `[EventType("ClaimSubmitted", Version=2)]` attribute, surfaced via `ReflectionTypeLoader` at startup. Becomes an O(1) dictionary lookup with no ReDoS surface, no length cap, no regex-timeout event id 9141. Deferred: requires coordinating a convention change with every publisher repo.
- **Story-9.3-SubscriptionStatusConfigured.** 4-state `HandlerSubscriptionStatus` enum (add `Configured` between `Unknown` and `Active`) to disambiguate "routing is set up but has never seen events" from "routing is set up and has seen events." Breaking change for downstream C# `switch` consumers. **Re-open trigger:** operator feedback post-landing indicates the 3-state model is ambiguous.
- **Story-9.3-ObservationStoreRebuildFromAuditLog.** Rebuild observation store from `AccessTelemetryLog` on startup to recover from sidecar-restart observation loss (Risk #8 degraded mode). Out of scope for 9.3.
- **Story-9.3-PostgresObservationStoreAlternative.** Investigate using an `AccessTelemetryLog`-backed Postgres VIEW in place of the dedicated Redis observation store — eliminates Redis write amplification (Risk #1) and sidecar-restart loss (Risk #8) in one move. Blocked until (a) `AccessTelemetryLog` backing is confirmed as Postgres and (b) a read-latency benchmark of the VIEW-based approach shows acceptable p95.
- **Story-9.3-CrossTenantVersionConsumerLookup.** A dedicated endpoint for publisher-owners to see "which tenants consume each version of my event type." Requires cross-tenant read permissions (operator-scope authZ). Deferred because the simpler tenant-scoped `VersionMismatch` detection satisfies the operational need inside 9.3; Epic 5 tenant-isolation invariant prevents a naive implementation.
- **Story-9.3-PostLaunchCategoryReview.** Measure 3 months of post-launch `memories.handlers.mismatches` counter data tagged by category; drop categories showing near-zero operator acknowledgement or >95% false-positive rate. Target review: 2026-09 or later. The three-category decision is explicitly revisitable based on measured telemetry, not speculation.
- **Story-9.3-Tier2IntegrationTests.** 9.3's Task 10 (Tier-2 Aspire-AppHost integration tests 10.0–10.12) was deferred during the initial landing pass. Unit coverage (45 new tests) pins the per-component invariants; the Tier-2 tests would add cross-component proof against a real Redis + DAPR sidecar. Specific deferred tests: `HandlersFixtureSmokeTests`, `HandlersListIntegrationTests`, `HandlersMismatchIntegrationTests` (VersionMismatch + healthy + StaleHandler), `ObservationStoreLostWrites_DetectorConvergesWithinTwoWindows` (property-based, dropProbability ∈ {0.0, 0.1, 0.3}), `HandlerEndpointLatencyNfrTests` (N=100 tenants, p95 500ms/200ms), `HandlerObservationKillSwitchIntegrationTests` (AC #21), `EventIngestionTelemetryAdapterSlowRedisTests` (AC #22 — bounded-FAF contract), `RedisObservedEventTypeStoreTests.ServerClockSkew_DoesNotPoisonWindow` (AC #26 — Finding N), `HandlerMetricsCardinalitySmokeTests`, `EndpointRoutingTests` + `MemoriesClientPathConstantTests` (AC #29 — Findings U, V), `HandlersListCommandTests.TableFormat_NoWrap_TruncatesWithEllipsis` + operator-polish tests (AC #30 — Findings B, X). Re-open trigger: first post-landing Tier-2 regression run or any cross-component bug report on 9.3 surface. Each test ~30s startup via Aspire fixture; budget ~1d to close the set.

## Deferred from: code review of 9-3-handler-registration-and-mismatch-detection (2026-04-25)

- **Source-prefix observation granularity / true per-handler fidelity.** The current observation store records tenant + aggregateType + eventType only; it does not persist the CloudEvent `source` or matched `SourceToTenantMap` prefix. As a result, `HandlerRegistryService` duplicates one tenant-wide observation set across every row, `StaleHandler` can only mean "tenant saw nothing" rather than "this configured sourcePrefix saw nothing", and `UnhandledEventType` remains heuristic versus the real longest-prefix router on `source`. Re-open trigger: first operator/reporting need for true per-`sourcePrefix` fidelity, or any follow-up story that revisits the observation-store write model.
- **Rolling 24h counts in `RedisObservedEventTypeStore`.** `GetObservedTypesAsync` currently reads last-seen membership from the zset and counts from a lifetime `HINCRBY` hash, so `eventsProcessedCount` and per-type counts are not truly window-bounded. Accurate rolling counts require a different write model (per-occurrence events, rolling buckets, or another substrate) rather than a small read-side patch. Re-open trigger: when the observation-store model is next revised — ideally in the same change as the source-prefix-fidelity fix so the write/read contracts only churn once.

## Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing committed-branch (2026-04-24)

- **F1 — Retry backpressure + 9174 + exponential backoff** (Task 8.5 / D2). `NaturalLanguageEmbeddingRetryHostedService.TickAsync` has no rate-limiter utilization check, no skip counter, no `9174` LoggerMessage, and no interval multiplier when `backlog > 1000`. Re-open trigger: rate-limiter consistently >80% utilization with retry queue contributing >20% of total calls.
- **F2 — Tier-2 / Tier-3 integration test suite for AC #14/#15/#16** (Task 9.1–9.6). `DualEmbeddingRoundTripTests`, `OutOfOrderEventTests`, `DegradedNaturalLanguageEmbeddingTests`, `CorrelationRootEdgeTests`, `IngestionWorkflowReplaySafetyTests`, consistency verification NL cases. Deferred per Task 9 header; re-open when Tier-2 environment is stable.
- **F3 — `RateLimiterSizingValidator` + event 9163** (Task 8.7). Needed when first `SourceType.Event` ingest hits an under-sized tenant configuration. Re-open trigger: first production `9162` warning or tenant NL-pipeline SLO breach.
- **F4 — `retry-nl-embeddings` CLI dead-letter surface** (Task 8.8). Re-open when dead-letter volume > 0 in any tenant for >24h.
- **F5 — Logprobs extraction for confidence promotion** (Task 2.5 / D1). Blocked on Dapr.AI 1.17.6 SDK surface. Re-open when `ConversationClient` exposes `logprobs` or the equivalent shape on `ConversationResponse`.
- **F6 — Per-tenant LLM configuration.** Phase 2. MVP is single system-wide `conversation.openai` component; operators swap via YAML.
- **F7 — `NaturalLanguageEmbeddingRetryHostedService.ScheduleRetryAsync` orphaned-workflow dead-letter.** When `ScheduleNewWorkflowAsync` + `WaitForWorkflowCompletionAsync` loop times out repeatedly for the same record, move to dead-letter after N ticks rather than leaving stuck instances in queue.
- **F8 — Redis cluster multi-node enumeration in `FailedNaturalLanguageEmbeddingRegistry.ListTenantsWithBacklogAsync`.** Current `GetFirstConnectedServer()` covers single-node / replicated deployments only. Cluster deployment re-open trigger: moving to Redis Cluster in production infrastructure.
- **F9 — `OrphanSemanticIndexReconciler` interval-based re-run.** Currently one-shot startup sweep only (per D3 decision pending). Re-open if post-startup SIGKILL-during-provisioning produces orphan NL indexes in production.
- **F10 — `IsStubBackfillMigration` atomic gate-write + backfill safety.** Partial-commit risk after backfill if `MERGE SchemaMigration` throws. Defer to ops runbook monitoring; re-open if migration re-runs cause operator friction.

## Deferred from: code review of 1-3-content-extraction-via-kreuzberg (2026-03-28)

- **DataContract/DataMember attributes missing on V1 contracts** — Systematic gap across all V1 contracts (ExtractionInput, ExtractionResult, MemoryUnit, GraphEdge, etc.). None use DataContract/DataMember/JsonPropertyOrder/JsonConstructor attributes per project-context.md rules. Should be addressed as a batch across all V1 types.
- **No transient/permanent exception classification for Kreuzberg errors** — AC4 is met (exceptions propagate for DAPR Workflow retry). However, permanent failures (corrupt files) will be retried indefinitely. Future work: classify KreuzbergValidationException as non-retriable, KreuzbergOcrException as retriable.
- **Large byte[] in ExtractionInput persisted to workflow state store** — DAPR Workflow serializes activity inputs to state store. For 1MB files, this means ~1.33MB base64 per workflow instance. Accepted per D13 (MVP payloads ≤1MB). Future work: consider streaming or external storage for larger payloads.
- **byte[] mutable on immutable record** — ExtractionInput uses byte[] which is mutable, breaking record immutability semantics and reference-based equality. No practical alternative exists in .NET for JSON-serializable binary data (ImmutableArray/ReadOnlyMemory don't serialize well).

## Deferred from: code review of 1-4-embedding-generation (2026-03-29)

- **End-to-end embedding flow is not wired into orchestration** — Deferred because orchestration and memory-unit persistence belong to upcoming ingestion workflow work and depend on the final pipeline shape.
- **Rate-limiting scope conflicts with credential scope** — Deferred because Story 1.7 introduces provider configuration and is the right place to decide per-tenant vs per-credential quota enforcement.
- **Story transition rationale is comment-only and not machine-readable** — The sprint tracking update relies on a free-form YAML comment for rationale, so tooling cannot query or validate why a story moved between workflow states.
- **Story status requires manual dual-write across tracking files** — The workflow duplicates status in both the story artifact and `sprint-status.yaml`, which is a pre-existing consistency risk whenever one file changes without the other.

## Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-29)

- **ValidateResult.IsValid/ErrorMessage is dead code on failure path** — ValidateContentActivity throws exceptions for invalid input; the ValidateResult record is only used for the success path. Spec mandates this contract shape, so keeping as-is.
- **SaveDedupKeyActivity: no TTL on dedup keys** — Dedup keys persist forever in DAPR state store, preventing re-ingestion of deleted content. Cleanup mechanism belongs to Epic 8 (Story 8.2 consistency verification).
- **ContentBytes serialized inline in DAPR workflow state** — Base64-encoded byte[] in IngestionInput causes replay amplification for large files. Accepted per D13 (MVP payloads <= 1MB). Same issue as Story 1.3 ExtractionInput. Future: external blob storage for content.

## Deferred from: code review of 1-6-ingestion-workflow-orchestration (2026-03-30)

- **Duplicate dedup entries are returned without confirming the referenced memory unit still exists** — The workflow fast-returns on a dedup hit without verifying that the stored `MemoryUnitId` is still present in the indexed backends, so manual cleanup or backend drift can leave callers with a duplicate response that points at missing data.

## Deferred from: adversarial code review of 1-6-ingestion-workflow-orchestration (2026-03-30)

- **indexedAt set to ingestedAt in GraphQueryBuilder** — `BuildMergeMemoryUnitNode` sets the FalkorDB `indexedAt` property to the workflow's `ingestedAt` timestamp. These are semantically different (when ingestion started vs when the graph write happened). Fixing requires adding a separate `indexedAt` parameter to `IndexInput`, which is a cross-story contract change (Story 1.5).
- **CaseId not validated for special characters** — `TenantId` has a strict alphanumeric+hyphen regex via `TenantIdGuard.Validate`, but `CaseId` only checks for null/empty. Not spec-required; CaseId is used as hash field values (not key names or graph names), so the blast radius is limited to potential key scan interference.

## Deferred from: code review of 2-6-explain-mode-and-confidence-scores (2026-04-02)

- **Return `offset` and `maxResults` pagination metadata in search response envelopes** — AC 3 still calls for `offset`, `maxResults`, and `totalCount` in paginated responses, but the response contracts still expose only `TotalCount`. This appears to predate the explain-mode change and would require a broader response-contract update across `SearchResult` and `HybridSearchResult`.

## Deferred from: code review of 2-7-benchmark-suite-and-thesis-validation (2026-04-12)

- **InternalsVisibleTo in packable library without strong-name key** — `Hexalith.Memories.Redis.csproj` has `IsPackable=true` and `InternalsVisibleTo` for Benchmarks without a strong-name key. Any consumer assembly named `Hexalith.Memories.Benchmarks` could access internals. Pre-existing pattern across the project; low practical risk.
- **FusionEngine non-finite handling asymmetry across axes** — Graph axis skips non-finite scores entirely (`continue` in FusionEngine), while syntactic/semantic axes normalize non-finite to 0.0 via ScoreNormalizer. Both paths produce safe results, but the mechanism differs: a document with a NaN graph-only score is excluded from fusion, while a NaN syntactic-only score becomes 0.0 and is included. Defensible design — graph scores bypass normalizer.

## Deferred from: code review of 3-1-create-and-list-cases (2026-04-12)

- **Case creation is non-atomic across Redis and FalkorDB** — `CreateCaseAsync` writes the Redis hash before creating the FalkorDB case node, so a graph failure can leave a Redis-visible phantom case. The story already records this as an accepted MVP gap, so it remains deferred for now.

## Deferred from: code review of 3-2-case-status-and-activity.md (2026-04-12)

- **Case creation is non-atomic across Redis and FalkorDB** — `CreateCaseAsync` still writes the Redis hash before creating the FalkorDB case node, so a graph failure can leave a Redis-visible phantom case. This remains a pre-existing MVP gap from Story 3.1 rather than a regression introduced by Story 3.2.

## Deferred from: 3-3-case-member-management (2026-04-12)

- **Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key** — Story 3.3 introduces a `:members` Redis Hash key per case for member storage. When Story 3.5 implements case deletion, it must also delete this key alongside the case hash and `:activity` stream to avoid orphaned data.

## Deferred from: 3-5-memory-unit-deletion-and-case-deletion (2026-04-12)

- **Dedup key orphaning after MU deletion** — Deleting a memory unit removes it from RediSearch, Redis Vector, and FalkorDB, but the DAPR state store dedup key persists. Re-ingesting identical content is silently blocked by dedup detection, returning a stale MU ID. Fix: add dedup key TTL or explicit dedup key deletion during MU deletion. Belongs in Epic 8 (Story 8.2 consistency verification).
- **Ingestion workflow must check `CaseStatus.Deleting`** — Story 3.5 sets case status to `Deleting` during case deletion, but the ingestion workflow (`ValidateContentActivity`) does not yet check this status before creating CONTAINS edges. A concurrent ingestion during case deletion could create orphaned MUs. Wire the status check into ingestion validation.
- **Story 3.6 must extend `DeleteMemoryUnitAsync` for annotation cascade** — Story 3.5's `DETACH DELETE` removes `annotates` edges but leaves connected annotation MU nodes intact. When Story 3.6 implements annotations, `DeleteMemoryUnitAsync` must first traverse outgoing `annotates` edges, recursively delete annotation MUs, then delete the target MU.

## Deferred from: code review of 3-4-case-scoped-and-cross-case-search (2026-04-12)

- **metadataQuery no length/content validation** — The `metadataQuery` query parameter has no length limit or format validation at the endpoint level, unlike `sourceType` (enum-validated) and `caseId` (existence-checked). General input validation concern across all query parameters. [Program.cs:436]
- **cancellationToken not propagated in ResolveNamesAsync** — `CaseService.ResolveNamesAsync` accepts a CancellationToken but never passes it to Redis batch operations or Task.WhenAll. StackExchange.Redis batch ops have limited cancellation support; pre-existing pattern in other batch methods. [CaseService.cs:321]
- **No input validation on caseId format before Redis key construction** — `caseId` undergoes no format validation (unlike `tenantId` which has `TenantIdGuard`). A caseId containing `:` is used directly in Redis key patterns. Defense-in-depth gap, though read-only lookups limit blast radius. [Program.cs:472]
- **No error handling for Redis failure in case name enrichment** — If Redis fails during the optional `ResolveNamesAsync` call, the entire search request returns 500 even though core search results are already available. Should degrade gracefully by returning results without case names. [Program.cs:988]

## Deferred from: code review of 5-5-tenant-configuration-and-listing (2026-04-14)

- **Keep actor-proxy fallback for tenant summaries instead of forcing the Task 1.6 state-store bypass** — Deferred by review decision. Reason: state-store key format is not empirically verified yet, so the actor fallback is the safer MVP path for now. [src/Hexalith.Memories.Server/Program.cs:1829]
- **Breaking-change conflict response still returns the wrong error contract** — `CreateEmbeddingConfigConflictResponse` still emits `error = "EmbeddingConfigChangeRequired"` instead of the pinned `EMBEDDING_CONFIG_BREAKING_CHANGE` response contract. This predates Story 5.5 and was not introduced by the current diff, so it remains deferred here. [src/Hexalith.Memories.Server/Program.cs:1888]

## Deferred from: code review of 6-4-pipeline-state-persistence-and-zero-data-loss (2026-04-16)

- **Per-run Docker named volumes are never torn down** — Fixture generates `hexalith-memories-it-<guid>` volumes for test isolation but nothing cleans them up. CI hosts accumulate disk usage over time. [src/Hexalith.Memories.AppHost/Program.cs:175-181]
- **`_logProvider` in the fixture accumulates entries across restart lifetimes** — `RestartTopologyAsync` does not reset the shared log provider, so any future test code that captures a pre-restart index and inspects post-restart entries will see mixed lifetimes. Latent trap rather than a current bug. [tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs]
- **`[DataMember]` attributes omit explicit `Name`** — Property renames on `CorpusStatistics`, `RateLimitState`, and `CaseIngestionCounts` will silently break wire format for existing persisted actor state. Set `[DataMember(Name = "...")]` explicitly before the next rename. [src/Hexalith.Memories.Server/Actors/CorpusStatistics.cs, RateLimitState.cs; src/Hexalith.Memories.Contracts/V1/CaseIngestionCounts.cs]
- **`BuildDedupKey` duplicates server-side hash logic in the test** — `PipelinePersistenceIntegrationTests.BuildDedupKey` recomputes `SHA256(sourceUri)` exactly the way the server does today. Any future change to URI normalization on the server will be invisible to the test. Replace with a server-side dedup-inspection query or an exposed helper. [tests/Hexalith.Memories.IntegrationTests/Ingestion/PipelinePersistenceIntegrationTests.cs:770-774]
- **AppHost token propagation uses process-env side effects** — `ApplyProcessEnvironmentTokens` seeds `APP_API_TOKEN` / `DAPR_API_TOKEN` into the AppHost process environment because CommunityToolkit.Aspire.Hosting.Dapr 9.7 does not expose a sidecar-scoped env API. Tokens leak to every child container/subprocess and are never unset. Revisit when the toolkit exposes a sidecar-env builder. [src/Hexalith.Memories.AppHost/Program.cs:183-198]

## Deferred from: code review of 7-2-output-formats-and-explain-display (2026-04-16)

- **Dictionary iteration order in `AxisDetails` / `Metadata` is not sorted** — `SearchExplanation.AxisDetails` and `MemoryUnit.Metadata` formatters enumerate the underlying `Dictionary<,>` in server-insertion order. Test-fragility for golden snapshots if the server ever reorders its JSON payload; no AC broken today. Sort keys at emit time when that becomes load-bearing. [src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, SearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs, MemoryUnitTableFormatter.cs]
- **`NaN` / `Infinity` confidence or composite scores poison the JSON envelope** — If the server ever emits non-finite floats, human/table prints `NaN`/`Infinity` and the JSON envelope emits bare `NaN` tokens that strict parsers (jq, `JSON.parse`, Python `allow_nan=False`) reject. Contracts don't currently enable `AllowNamedFloatingPointLiterals`; treat as contract-boundary work if non-finite scores ever become legitimate. [src/Hexalith.Memories.Cli/Output/Formatters/HybridSearchResultHumanFormatter.cs, MemoryUnitHumanFormatter.cs]
- **`IOutputFormatter<T>.Write` signature has no `CancellationToken`** — Broken downstream pipe (`memories … | head -1` on a large body) surfaces as "Unexpected error contacting Memories Server" rather than a clean broken-pipe exit; Ctrl+C during a synchronous write has no effect. Signature change is architectural and out-of-scope for 7.2. [src/Hexalith.Memories.Cli/Output/IOutputFormatter.cs]
- **`Uri.EscapeDataString` on path-segment IDs produces `%2F` for embedded slashes** — ASP.NET Core rejects `%2F` in path segments by default (404 Not Found), surfacing as an opaque "not found" error to the user. IDs containing `/` are unusual; the clean fix is CLI-side rejection or server-side `AllowEncodedSlashes`. Server concern. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:810]
- **`BuildSearchPath` drops subpath when `--endpoint` has one** — Constructing `"api/search?..."` as a relative URI against an `HttpClient.BaseAddress` like `http://host:5000/v1` drops `/v1` per `Uri` resolution rules. No 7.2 AC exercises subpath endpoints; Story 7.1 owns endpoint normalization. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs BuildSearchPath]
- **`--max-results abc` exits via System.CommandLine default error** — Non-integer input bypasses the 7.2 "plumbing = 2" exit-code contract because System.CommandLine's built-in parser emits its own error format. Parser-level concern consistent with the Story 7.1 baseline; neither story tests this edge. [src/Hexalith.Memories.Cli/Commands/SearchQueryCommand.cs Options]

## Deferred from: code review of 7-4-quickstart-and-documentation (2026-04-17)

- **`IngestAsync` returns `Task<string>` (workflow id) not `Task<MemoryUnit>` and takes `byte[]` + `contentType` + `ingestedBy`** — spec line 168 allowed "or equivalent — grep first" flexibility, but the signature divergence cascades into validation step's inability to match by `MemoryUnitId`. To be reconsidered during Group 2 (MemoriesClient) review of this story. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401]
- **Port availability check binds `IPAddress.Loopback` only; IPv6-only services missed, and bind-success is TOCTOU-advisory on Windows port reservations** — platform-specific caveat is already acknowledged in spec Task 2.4; upgrade to dual-stack check and/or `IpGlobalProperties.GetActiveTcpListeners` lookup when touched next. [src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs:182]
- **`EnsureSampleCaseAsync` picks first match of `DefaultCaseName` without stable ordering** — only manifests after repeated failed runs leave duplicate cases in the tenant. Low likelihood; will be neutralized if `CreateCaseAsync` is removed per the open decision. [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:276-283]
- **`NegativeCanaryQuery` is a literal constant — any future fixture copying the token into a sample body silently breaks the canary invariant** — add a startup self-check (assert `SampleDocumentText` does not contain the canary token) when touched next. [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:31]
- **`HealthStep` failure suggestion interpolates `result.LastError` raw** — may surface exception messages with proxy URLs or paths in CI logs. Low sensitivity for a dev wizard; sanitize when the wizard grows a non-local-dev use case. [src/Hexalith.Memories.Cli/Commands/QuickstartCommand.cs:349-354]
- **`DurationMs` uses `checked((int)Math.Round(...))` — overflows at ~24.85 days and would throw mid-serialization** — real-world risk negligible given the wizard's bounded 60s probe + 30s provisioning. Switch to `long` or clamp if the wizard ever grows unbounded polls. [src/Hexalith.Memories.Cli/Quickstart/QuickstartStepResult.cs:89]
- **`CreateCaseAsync` exceeds spec-authorized HXL001 surface (Story 7.4 TL;DR items 4-5)** — docs/dev/experimental-apis.md already lists all three HXL001 methods; the story spec should be amended (TL;DR item 5) to acknowledge `CreateCaseAsync`. Alternative: remove `CreateCaseAsync` and rely on server-side auto-create (requires server support). [src/Hexalith.Memories.Cli/Quickstart/QuickstartSampleFlow.cs:285-289, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:341]
- **`PrerequisiteCheckResult.IsSkipped` is a 7.4 refinement beyond the spec-pinned 3-field signature** — provides clearer "advisory pass" UX (SKIP vs. OK-with-advisory). Spec Task 2.1 should be amended to acknowledge the 4-field record and the "SKIP for soft-fail" rendering convention. [src/Hexalith.Memories.Cli/Quickstart/PrerequisiteCheckResult.cs:18]

## Deferred from: code review of 7-4-quickstart-and-documentation — Group 2 (MemoriesClient) (2026-04-17)

- **`CreateTenantAsync` returns `Task<string>` and `IngestAsync` returns `Task<string>`, diverging from spec TL;DR items 4 and 5** (which called for `Task<TenantSummary>` and `Task<MemoryUnit>`). Both server endpoints are 202 Accepted fire-and-forget, so returning the workflow instance id is the honest contract; the spec signatures were drafted before the server surface was verified. Amend spec TL;DR items 4-5 to match and document the polling pattern (`CreateTenantAsync` → poll `GetTenantAsync` until `TenantStatus.Active`; `IngestAsync` → poll via the workflow id route) in the next spec touch. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:267, src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:451]
- **`IngestAsync` has no client-side content-size guard** — server rejects >1 MiB via `IngestionInputValidator` (returns `INVALID_INPUT`) and >2 MiB via `RequestSizeLimitAttribute` (returns 413 with no `ErrorResponse` body, so `ErrorResponseDecoder.DecodeAsync` yields a terse diagnostic). UX polish, not correctness — the quickstart sample is well under 1 MiB. Revisit if `IngestAsync` grows beyond quickstart scope. [src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:401]

## Deferred from: code review of 8-2-consistency-verification-and-repair (2026-04-20)

- **Semantic re-index remains intentionally unsupported in `SemanticIndexer`** — `SemanticIndexer.ReIndexFromSyntacticAsync` still throws `NotSupportedException`, so `ReIndexSemantic` / `ReIndexSemanticAndGraph` remain documented follow-up work rather than live repair paths. [src/Hexalith.Memories.Server/Consistency/SemanticIndexer.cs:84]

## Deferred from: code review fix pass of 8-3-data-export (2026-04-20)

- **Classification metadata is not persisted into the syntactic export source of truth** — `MemoryUnit.Classification` exists on the contract/export surface, but the ingestion/indexing path (`IndexInput`, `IndexSyntacticActivity`) does not write classification into the Redis memory-unit hash. `CaseService.ParseMemoryUnitFromHash` and `TenantExportService` therefore cannot recover it during export. Fix in a future story by persisting classification at ingest/index time, then plumb it through export. [src/Hexalith.Memories.Contracts/V1/IndexInput.cs, src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs, src/Hexalith.Memories.Server/Cases/CaseService.cs]

## Deferred from: 9-1-event-auto-discovery-and-dapr-pub-sub-subscription (2026-04-22)

- **Tier-3 Aspire end-to-end integration tests for EventStore subscription** — Task 6 guard tests that require a running DAPR sidecar + Redis + FalkorDB are deferred to a follow-up Tier-3 / nightly harness. This covers: `EventIngestionRoundTripTests` (publish via DAPR → search within 5 s, NFR6), `EventIngestionSubscriptionDiscoveryTests.DaprSubscribeEndpoint_ListsConfiguredTopic` + `Startup_FailsFast_WhenSubscribeEndpointEmpty`, `EventIngestionReplayAfterRestoreTests.ReplayedEvent_AfterTenantRestore_BlockedByIdempotency`, and `EventIngestionLatencyTests.SingleEvent_P50Under3s_Enforcement` / `SingleEvent_P95Under5s_Observation`. The existing Tier-1 (65 tests, `tests/Hexalith.Memories.EventStore.Tests/`) + Tier-2 (10 tests, `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/` — outcome mapping, middleware-order, documentation completeness) coverage pins every non-end-to-end guard test from the story's risk table. Rationale: standing up the full Aspire topology for one subscription round-trip is disproportionate to the risk the Tier-2 tests already cover at the controller / outcome-mapping level; the replay-after-restore case is behaviorally correct as long as `CheckIdempotencyActivity` runs (a Tier-2 concern covered by `CheckIdempotencyActivityTests`). The nightly Aspire harness is the right place to catch sidecar / broker wiring regressions.
- **Review findings from the planning validation (Review Findings block in the story file)** — Several \[Review\]\[Patch\] entries in the story's Review Findings section surfaced during planning iteration and were folded into Tasks 1-4 during implementation (controller `[Topic]` binding, EventStore package boundary with Server adapters, typed router outcomes, compensated hybrid dedup, response DTO with `instanceId` contract, configurable `PubSubName`, canonical middleware order, queryable subject metadata, severity-correct 9100-9129 log bank). Any remaining bullets that did not turn into task-level checkboxes are documentation refinements (e.g. AC #17 wording), not code-level deferrals, and can be cleaned up in a follow-up story pass. Touching the Review Findings block would violate the dev-story "only modify the story file in permitted areas" rule.

## Deferred from: code review of 8-4-end-to-end-telemetry-integration-tests (2026-04-22)

- **Activity / HttpResponseMessage using-scope pattern inconsistency in retry test** — `SearchOperation_RetrySequence_EmitsDistinctAuditEventsPerStatus` uses mixed `using` statement vs `using` statement scopes for `Activity retryRoot` vs `Activity secondAttempt`; the pattern is correct, just inconsistent. Pre-existing style nit, not caused by 8.4 scope boundary.

## Deferred from: 8-5-redis-otel-instrumentation (2026-04-23)

- **`OpenTelemetry.Instrumentation.StackExchangeRedis` prerelease pin — upgrade-on-GA trigger.** Package pinned at `1.15.1-beta.1` in `Directory.Packages.props` per ADR-8.5-001 (b). Revisit this pin within **14 days of `1.15.0`** (non-prerelease) shipping on nuget.org, **OR by 2026-09-30**, whichever comes first. Owner: Memories release-manager rotation. Review-by: **2026-09-30**. On review, either (a) bump to the GA version and remove the `-beta.N` tag from `Directory.Packages.props` + update `packageSourceMapping` comment, or (b) file a new deferred-work entry with a fresh review-by if GA is still not shipped. Tracking: ADR-8.5-001 (g). [Directory.Packages.props, NuGet.config, docs/dev/telemetry.md ADR-8.5-001]

## Deferred from: code review of 8-5-redis-otel-instrumentation (2026-04-23)

- **Malformed or truncated Redis breadcrumbs are still silently dropped by `ServerActivityStreamReader`.** `TryParse(...)` catches `JsonException` and returns `null`, so the Story 8.5 hard Redis-span assertion can report a missing span when the real failure is capture corruption / truncation. Deferred as pre-existing review debt in the existing stderr-breadcrumb reader path. [tests/Hexalith.Memories.IntegrationTests/Telemetry/Infrastructure/ServerActivityStreamReader.cs]

## Deferred from: 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)

- **Task 8.7 — `RateLimiterSizingValidator` (Winston promoted, Improvement AB).** Validator emits `9163 RateLimiterUnderSizedForEvents` at Warning when a tenant's `EmbeddingRateLimiterActor` ceiling is below `sustainedUsage * 2` over a 15-min sliding window. Core dual-embedding path ships without the validator; the degraded-state queue path already protects against cascade failure under doubled API volume. Follow-up: add `RateLimiterSizingValidator.cs` that reuses the retry hosted service's scheduling slot, plus unit tests `.SustainedUnderSizing_Emits9163 / .TransientBurst_DoesNotEmit / .CeilingSufficient_DoesNotEmit`.
- **Task 8.8 — `memories retry-nl-embeddings` CLI surface (Improvement F).** Dead-letter inspection + re-enqueue is interim via `redis-cli` commands (documented in operator runbook section 10.1.3). Follow-up: add the sub-command to `Hexalith.Memories.Cli` once that project surfaces — current story scope ships no CLI project. Interim commands: `redis-cli ZCARD nl-embedding-retry-dead:{tenant}` + `redis-cli ZRANGEBYSCORE ... | xargs redis-cli ZADD nl-embedding-retry:{tenant} ...`.
- **Task 9.1 — `DualEmbeddingRoundTripTests` (Tier 2/3).** Publishes a test CloudEvent via `DaprClient.PublishEventAsync` and polls both raw + NL hashes for dual indexing within 7s. Requires Aspire `DistributedApplicationTestingBuilder` + DAPR slim + Redis + FalkorDB + `conversation.echo` component. Follow-up: add under `tests/Hexalith.Memories.IntegrationTests/Ingestion/`.
- **Task 9.2 — `OutOfOrderEventTests` (Tier 2/3).** Publishes event B (with `causationid = A_id`) before event A; asserts stub node is created with `isStub = true` + `stubCreatedAt` set; publishes A; asserts stub promoted (`isStub = false`, `9154 StubNodeResolved` emitted). Requires FalkorDB integration fixture + replay window.
- **Task 9.3 — `DegradedNaturalLanguageEmbeddingTests` (Tier 2, 3 scenarios).** Scenario A: LLM transient failure → `Queued` + retry completes on next tick. Scenario B: index-side partial failure → workflow-level retry recovers. Scenario C: index-side terminal failure → compensation drops both hashes. Requires NSubstitute-replaceable DAPR Conversation client + fault injector for `IndexNaturalLanguageSemanticActivity`.
- **Task 9.4 — `CorrelationRootEdgeTests` (Tier 2 FalkorDB fixture).** Publishes 1 root + 3 correlated events; asserts root has no self-edge, each correlated event has exactly one edge from root, no edges between correlated events, `9155` emitted once. Guard for Risk #3 at the integration level — the unit test already covers the activity-level behavior via `IndexGraphActivityTests.CorrelationId_CreatesRootToCurrentEdge` + `.CorrelationIdEqualsMemoryUnitId_NoSelfEdge_LogsDebug`.
- **Task 9.5 — `IngestionWorkflowReplaySafetyTests` (Tier 1).** Simulates a 9.1-shape history replaying under 9.2 code. Requires fabricating a `durable-task` state snapshot — the SDK surface for this is undocumented. Document the failure mode as deterministic (it is) and rely on the runbook quiesce (AC #11) + `WorkflowReplaySafetyHostedService` startup gate (Task 5.9) as the combined mitigation.
- **Per-tenant LLM provider configuration (Phase 2 per architecture L1254).** MVP ships one system-wide `llm` DAPR Conversation component. Operators swap providers by editing `deploy/dapr/components/conversation-llm.yaml` (no code change required). Per-tenant LLM is tracked as a Phase 2 follow-up.
- **Content-absent fallback in `GraphTraversalService`** (`src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:~95-100`). Retire after the Task 7.6 `IsStubBackfillMigration` has been executed against all production databases. Until then, the fallback MUST be kept — pre-9.2 stubs have neither the `isStub` flag nor content.
- **Integration tests for `NaturalLanguageSemanticSearchService`.** The library class ships without being wired into `HybridSearchService` (AC #7 — staged rollout). Follow-up story wires it in behind an opt-in `axis=naturalLanguage` query parameter and adds end-to-end search tests.

## Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing (2026-04-24)

- **Workflow-version metadata threading for replay-safety startup gate (AC #11 / Task 5.9).** `Dapr.Workflow.WorkflowState` (SDK 1.17.6) does not expose a code-version field per active instance, so `ShouldCountWorkflow` falls back to "any in-flight IngestionWorkflow." Clean same-version redeploys wait for the drain window (up to 5 min). Follow-up: (a) investigate SDK surface across Dapr 1.18+ for a workflow-version metadata hook, (b) if unavailable, thread a `"workflowCodeVersion"` tag through `IngestionInput` and persist to workflow state so the gate can compare against `Assembly.GetEntryAssembly().GetName().Version`. AC #11 updated 2026-04-24 to document the relaxation.

- **Risk-mapped guard tests that depend on integration topology** — `DualEmbeddingLatencyTests` (Risk #2, P95 benchmark), `DaprConversationIntegrationTests.ApiSurfaceSmokeTest` (Risk #11), `GraphQueryBuilderTests.BuildMergeStubNode_OnExistingNonStub_DoesNotRegressIsStubFlag` (Risk #12 Tier-2 FalkorDB fixture), `EmbeddingInputReplaySafetyTests.PreNineTwoEmbeddingActivityHistory_ReplaysSuccessfully` (Risk #17). All bound to the Task 9.x integration-test deferral above.
- **Risk #3 coverage gap at unit level** — `IndexGraphActivityTests.MultipleEventsSameCorrelationId_NoFanOut` and `GraphTraversalServiceTests.CorrelatedWith_InboundDirection_ReturnsCorrelatedSiblings` — the fan-out-prevention and inbound-traversal tests name-listed in Risk #3 that go beyond the two shipped tests. Tied to Task 9.4 `CorrelationRootEdgeTests`.
- **Risk #4 gap-marker unit tests** — `GraphTraversalServiceTests.ExplicitIsStubTrue_IdentifiesGapMarker` + `.ExplicitIsStubFalse_IncludedInTraversal` + `.ContentAbsentHeuristicFallback_ForLegacyNodes`. Tied to Task 9.2 `OutOfOrderEventTests`.
- **Risk #6 unit tests** — `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` + `EmbeddingRateLimiterActorTests.BothContentKinds_ConsumeSameBudget`. Tied to Task 8.7 `RateLimiterSizingValidator` deferral (Risk #6 guard + follow-up).
- **Risk #9 follow-up tests** — `MultipleTenantsWithBacklog_FairlyDequeuesAcrossTenants`, `RestartMidIteration_DoesNotDoubleScheduleSameRecord`, `NaturalLanguageEmbeddingRetryWorkflowTests.Idempotency_DuplicateScheduling_DoesNotDoubleIndex`. Tied to Task 9.3 `DegradedNaturalLanguageEmbeddingTests`.
- **Orphan stub operator surface** — `OrphanStubQuery_ReturnsStubsOlderThanThreshold` test + `memories_graph_orphan_stub_count{tenant}` gauge + `memories graph orphan-stubs --tenant X --age 24h` CLI sub-command (Dev Notes "Orphan stub detection"). Tied to Task 8.8 CLI project deferral.
- **Task 7.1 reflection test** — `GraphQueryBuilderTests.AllCallers_PassStubCreatedAt` (reflection-based enumeration of `BuildMergeStubNode` callers asserting 2-arg form). Tied to the patch-level deprecation of the 1-arg overload.
- **Task 1.9 Improvement AD dynamic-compilation `ProjectCompilationTests`** — diff ships the weaker file-content-string form (`File.ReadAllText` + regex assertions). The stronger dynamic-compilation variant that builds a throwaway project and asserts zero `DAPR_CONVERSATION` diagnostics is deferred; current form still catches the regression the Improvement AD cared about.

## Deferred from: Session 5 — 9-2 review follow-up (2026-04-24)

- **D1 — Logprobs-based confidence extraction in `GenerateNaturalLanguageDescriptionActivity`.** Dapr.AI 1.17.6 `ConversationResultChoice` exposes only `FinishReason`, `Index`, and `Message` (verified against `C:/Users/.nuget/packages/dapr.ai/1.17.6/lib/net9.0/Dapr.AI.xml`). The SDK does not surface per-token `logprobs` from the underlying provider response, so the spec-documented `exp(avg(logprob))` computation has no upstream to pull from. Task 2.5 permanently documents `ConfidenceSource = Constant` + `EstimatedConfidence = null` as the MVP behavior. Re-open triggers: (a) Dapr.AI exposes logprobs on `ConversationResultChoice` or an extension surface, OR (b) a follow-up story ships a direct-provider client path (bypassing DAPR) that exposes logprobs — only relevant if operator UX research confirms users want measured confidence numbers. Evidence in comments at `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs:~206`.

- **Retry backpressure `9174 RetryBackpressureOverride` deferred.** Decision D2 was originally resolved "implement now" but on inspection the `EmbeddingRateLimiterActor` public surface does not currently expose a read-side "budget utilization %" API that the retry hosted service can consume without a new actor method + DI wiring. Shipping a partial implementation risks either (a) always-dequeue (false-negative backpressure) or (b) always-skip until manual bypass (stampede when LLM recovers). Re-open trigger: add `EmbeddingRateLimiterActor.GetCurrentUtilizationAsync()` as part of a follow-up Task 8.5.1, then layer the skip-counter + 9174 override + exponential backoff over the top. The current hosted service's `9170/9179` backlog Warning/Error + the underlying rate-limiter's throttling on the LLM calls themselves cover the acute failure mode (doubled API volume rate-limited at the embedding layer).

## Deferred from: code review of 9-2-dual-embedding-and-causal-chain-indexing Session 6 (2026-04-25)

- **S6-F1. Comparer-rebuild guard test/analyzer for `new Dictionary<string, MetadataField>(...)` without explicit comparer.** D6 normalizes Ordinal at the contract boundary (`IngestionInput.Metadata` / `IndexInput.Metadata` `init` accessors), but intermediate copies that build a fresh `Dictionary` without passing the comparer escape the safety net. Follow-up: add a unit test that scans the server project sources for `new Dictionary<string, MetadataField>` calls and asserts each one passes a comparer, OR introduce a Roslyn analyzer.
- **S6-F2. Replace `LogGateFailedOpen` side-comment contract with explicit outer-caller log on null return.** `WorkflowReplaySafetyHostedService.cs:75-77` currently relies on a code comment that "TryCountInFlightAsync already logged" — fragile to inner-method refactors. Follow-up: add an outer "gate-bypassed" Critical log whenever the count is null, regardless of inner-method logging.
- **S6-F3. Replace reflection-on-private-static in `BuildIndexMetadata_*` test with `internal` + `InternalsVisibleTo`.** Test currently uses `BindingFlags.NonPublic | BindingFlags.Static` to reach `IngestionWorkflow.BuildIndexMetadata`. Follow-up: mark the method `internal`, add `InternalsVisibleTo` to `Hexalith.Memories.Server.Tests`, and rewrite the test to call directly.
- **S6-F4. Split `LogDiscrepancyDetected` emission for note-only vs. true-discrepancy cases.** Notes now flow through the same Warning logger event as discrepancies, inflating volume for healthy tenants during NL rollout. Follow-up: split into `LogDiscrepancyDetected` (Warn) + `LogConsistencyNoteObserved` (Info/Debug).
- **S6-F5. Tighten OCE `when` filter at `WorkflowReplaySafetyHostedService.cs:177-181` to log per-call timeouts even during host shutdown.** Currently `when (!cancellationToken.IsCancellationRequested)` swallows the per-call timeout when outer cancellation is concurrent. Follow-up: track the per-call CTS source explicitly and log if it was the cause regardless of outer state.
- **S6-F6. Move `MetadataField = typeof(WorkflowState).GetField(...)` from static cctor to `Lazy<FieldInfo?>` with try/catch + 9173 emission.** A missing/version-mismatched `Dapr.Workflow` assembly currently throws `TypeInitializationException` on first hosted-service invocation, bypassing the 9173 path entirely. Follow-up: lazy + structured log.
- **S6-F7. Map free-text-only `ConsistencyNote` (kind=None, note≠empty) to a typed sentinel in `BuildConsistencyNoteKind`.** Consumers cannot pattern-match on `None`. Follow-up: extend `BuildConsistencyNoteKind` to return `ConsistencyNoteKind.Other` (or similar) when free-text is present without a kind.
- **S6-F8. Add inner-loop deadline check to `WorkflowReplaySafetyHostedService.TryCountInFlightAsync`.** With 100-instance pages and 10s per-query timeout, hundreds of active instances can outlive the documented 5-min `TotalTimeout`. Follow-up: thread the outer deadline through the inner enumeration.
- **S6-F9. Add `BatchSize=1` accumulator test for the notes/discrepancies routing across batch boundaries.** The current `[10_001]` test always bumps batches in chunks of 64 (default); a `BatchSize=2` mixed-result test would catch carry-over off-by-one.

- **S6-FA. Operator-runbook PR — 9173 multi-reason documentation + Notes-split documentation in `docs/dev/consistency.md`.** Resolves S6-D4 (9173 EventId now overloaded with `workflow-name-reflection-null` / `sidecar-query-timeout` / `sidecar-query-exception` / `metadata-field-missing`) and S6-P15 (consistency.md still describes Discrepancies as the all-encompassing list, missing the Notes split + independent-cap behavior). One docs PR covers both: enumerate the four 9173 reasons + per-reason triage steps; update the consistency.md "Discrepancies" section to describe the structural split and the new EventId 8210 NotesListTruncated.

- **S6-FB. Extend `LogDiscrepancyDetected` (EventId 8201) with an `EntryKind` parameter (Discrepancy | Note) so operators consuming the spec-documented truncation-fallback log can disambiguate Note entries from Discrepancy entries.** Currently the `{Recommendation}` field is overloaded with `ConsistencyNoteKind` strings (e.g., `Recommendation NaturalLanguageEmbeddingMissing`) for note-only entries. This is a wire-shape change for log consumers; should land alongside S6-FA so the operator runbook can be updated atomically.

- **S6-FC. Add `ConsistencyVerificationResult_RoundTripsThroughMemoriesJsonContext` test asserting `Notes`, `NoteCount`, `TotalNoteCount` round-trip via the source-gen path.** Existing CLI JSON round-trip already exercises these properties end-to-end; the standalone source-gen contract test is helpful but not blocking. Track in `tests/Hexalith.Memories.Contracts.Tests/V1/ConsistencyContractSerializationTests.cs`.

- **S6-FD. Add `TryGetWorkflowName` end-to-end test that constructs a real `Dapr.Workflow.WorkflowState` (or a credible test double) and exercises both the happy path AND the `MetadataField is null` short-circuit.** Constructing `WorkflowState` requires test-double scaffolding the SDK does not document; gated on Story 9.x integration-test infrastructure.

- **S6-FE. Normalize the `ConsistencyVerificationResultHumanFormatter` "notes: none" / "Notes:" header layout across the discrepancy-empty / discrepancy-populated branches.** Cosmetic; minimal operator impact.

- **S6-FF. Strengthen the CLI table-formatter tests with `Shouldly.Case.Sensitive` + numeric-value assertions for the `notes`/`discrepancies` columns.** Existing tests pass against the corrected D6 column semantic; this is a test-quality follow-up.
  <!-- End of deferred items -->

## Deferred from: code review of stories 11-1 + 11-2 (2026-04-26)

- **W1 [resolved in 14.2]. SHA-pin actions in `release.yml`.** All five third-party `actions/*` references in `.github/workflows/release.yml` are now pinned to a 40-char commit SHA with a trailing `# v<x.y.z>` comment for review context. `CiTestInventoryTests.ReleaseWorkflow_ThirdPartyActions_ArePinnedToCommitSha` enforces the SHA shape so a future bump back to a floating tag fails the test.
- **W2 [resolved in 14.2]. `validate-release-packages.ps1` doesn't enforce non-Packable inventory completeness.** The validator now iterates every `src/**/*.csproj`, requires an explicit `<IsPackable>true|false</IsPackable>` declaration, and asserts every project is in exactly one of `packages` or `nonPackableProjects`. Coverage in `tests/tooling/release_packages/release_packages_test.py` exercises missing/unexpected/duplicate inventory entries and missing/blank/unsupported `IsPackable` values.
- **W3. `tools/test.sh` Python heredoc has no error path if `python3` is missing.** Linux/macOS runners always have it; Windows uses `test.ps1`. Add `command -v python3` guard with a clear error if Linux/macOS distros are added later that omit it. (`tools/test.sh:134`)
- **W4. Python `{*}Counters` namespace XPath requires Python ≥ 3.8.** Current ubuntu-latest ships 3.10+; document as a runner-version floor or hardcode the TRX namespace `http://microsoft.com/schemas/VisualStudio/TeamTest/2010`. (`tools/test.sh:140`, `tools/verify-integration-fast-coverage.py:54`)
- **W5. `if-no-files-found: error` + `if: always()` upgrades a build failure to two red checks.** Noisy but correct; reviewers must read the build step first. Resolve by gating artifact upload on `success() || failure()` against the test step specifically. (`.github/workflows/ci.yml:67-74,108-115`)
- **W6. `submodules: recursive` cannot fetch private submodules without PAT.** `Hexalith.Commons` and `Hexalith.EventStore` are public today; revisit if either becomes private. (`.github/workflows/ci.yml:30,52,84`; `release.yml:24`)
- **W7. `Substitute.For<WorkflowActivityContext>()` may fail if Dapr.Workflow seals the type in a future SDK.** Works against 1.17.6; failure mode is loud (NSubstitute throws at instantiation). (`tests/Hexalith.Memories.IntegrationTests/Telemetry/AspireEndToEndTraceTests.cs:330`)
- **W9. CONTRIBUTING.md skip wording (`Requires Docker - see CONTRIBUTING.md`) is documented but not wired into any test SkipAttribute.** Spec text is aspirational, not enforced as a contract. Wire it via a custom SkipAttribute when Docker-required local skips are needed. (`CONTRIBUTING.md:76-81`)
- **W11. `branch-protection.md` is a manual checklist with no automation.** Commit a `.github/rulesets/main.json` and a daily audit workflow. Out of scope for 11.x. (`docs/dev/branch-protection.md`)
- **W12 [resolved in 14.2]. `tools/release-packages.json` has no `$schema` reference.** New `tools/release-packages.schema.json` now defines required keys, `additionalProperties: false`, and `pattern`/`minItems` constraints. The inventory references the schema via `$schema` and `validate-release-packages.ps1` invokes `Test-Json -SchemaFile` before any structural use, so misspellings such as `packageID`, `projectPath`, or `nonPackableProject` fail loudly before pack/publish scripts run.
- **W13. `Cli/README.md` pre-announces the global tool before first publish on nuget.org.** Either ship the tool first, or document `--prerelease` until 1.0.0 lands. (`src/Hexalith.Memories.Cli/README.md:7-9`)
- **W14. CI workflow `fetch-depth: 0` not set on PR checkout.** commitlint isn't run in CI yet (only locally per CONTRIBUTING); add when CI adopts commit validation. (`.github/workflows/ci.yml:28-30`)
- **W15 [resolved in 14.2]. `validate-release-packages.ps1 -Version` regex not enforced inside the script.** `ConvertTo-NormalizedNuGetVersion` strips `+...` build metadata before equality compare, emits a `Note:` diagnostic naming both the original and NuGet-normalized form, and threads the normalized value through both the per-package version assertion and the internal cross-package dependency-version assertion. Coverage: `test_version_with_build_metadata_normalizes_with_clear_message` in `tests/tooling/release_packages/`.
- **W16 [partially resolved in 14.2]. `Where-Object {-notlike *.snupkg}` masks regression risk.** `tools/validate-release-packages.ps1` now uses `Where-Object { $_.Extension -ieq '.nupkg' }` for explicit extension matching. The mirror in `tools/publish-nuget.ps1:40-42` is intentionally not touched in 14.2 because the story's file scope only permits a `publish-nuget.ps1` edit when there is a concrete partial-publish gap; cosmetic alignment alone does not meet that bar. Re-open trigger: a partial-publish recovery story that already touches `publish-nuget.ps1`, or first .snupkg-symbol introduction.
- **W17. `verify-integration-fast-coverage.py` exit codes don't distinguish "missing surface" from "tool error".** Both yield exit 1; use distinct codes (e.g., 2 for parse error, 3 for empty results, 1 for missing surfaces). (`tools/verify-integration-fast-coverage.py`)
- **W18. CI `runs-on: ubuntu-latest` is unpinned.** Works today; pin to `ubuntu-22.04` if Docker engine version drift causes Testcontainers regression. (`.github/workflows/ci.yml`)
- **W19 [resolved in 14.2]. `concurrency: cancel-in-progress: false` enables stuck-release deadlock with `--skip-duplicate` self-heal.** Story 14.2 keeps `cancel-in-progress: false` deliberately because cancelling a release mid-publish would convert a recoverable partial-publish into an indeterminate half-state — `tools/publish-nuget.ps1 --skip-duplicate` rerun-and-self-heal recovery requires that the in-flight release runs to completion. The 30-minute job timeout and the partial-publish issue alert (S11-FD) bound the worst-case stuck-release window. `CiTestInventoryTests.ReleaseWorkflow_Concurrency_PreservesPartialPublishSelfHeal` enforces the policy so a future flag flip lands with an explicit recovery model rather than a silent edit.
- **W20. Release workflow runs build+restore+test+pack twice.** Pre-release validation + semantic-release internal pack pipeline duplicate work; optimize when CI minutes become a constraint.
- **W21. `tools/test.sh` Slow/Integration arms collapsed via `|`.** Functionally correct today (same project list); diverges from `test.ps1`. Resync if Slow ever gets its own list. (`tools/test.sh:79`)
- **W22. `PublicContractSerializationCoverageTests` uses name-suffix filter (`Validator`/`Defaults`/`Taxonomy`).** Fragile but works today; replace with `[ExcludeFromContractCoverage]` attribute when a false-positive is observed. (`tests/Hexalith.Memories.Contracts.Tests/V1/PublicContractSerializationCoverageTests.cs:54-58`)
- **W23. `CiTestInventoryTests` uses `Contains` for workflow text.** Too permissive; a future workflow refactor adding another `dotnet test` step with the wrong arguments still passes the assertion. Replace with structural YAML parsing. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:66-68`)
- **W24. `CiTestInventoryTests` opaque error if `RepoRoot` AssemblyMetadata missing.** Minor diagnostic improvement; emit a wire-up hint message. (`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:79-84`)
- **S11-FB. Compile-time symbol verification for `tools/integration-fast-required-surfaces.txt` (review patch P9).** Currently the verifier surfaces missing classes only after CI runs the lane. Promoting the check to compile-time requires either (a) a `ProjectReference` to `Hexalith.Memories.IntegrationTests` from a Docker-free test project (pulls integration deps), (b) a refactor of the surfaces file into a typed C# inventory consumed by both the verifier (after build) and a unit test, or (c) a `dotnet test --list-tests` step in CI before the test run. Re-open trigger: first surface drift incident, or the next time the surfaces list grows past ~10 entries.
- **S11-FC. Pre-flight stale-tag detection on release.yml (review patch P16).** `tagFormat: "v${version}"` collides with stale tags from manual or aborted releases. Currently the natural `git push tag` failure is the gate. Adding a structured pre-flight requires running `npx semantic-release --dry-run` to compute `nextRelease.version` (wasteful on every release) or carrying our own version-computation logic. Story 14.2 reassessed and chose to carry forward — neither preflight option meets the cost/benefit bar without an observed collision. Re-open trigger: first stale-tag-collision incident on `main`, migration from another release tool that left tags behind, or a release-time decision that the dry-run cost is acceptable. Defer-by: 2026-08-04 (re-evaluate at the next release-pipeline hardening pass if no triggering incident occurs).
- **S11-FD [resolved in 14.2]. Partial-publish alerting on the release pipeline (review decision D4).** Story 14.2 audited the existing path and confirmed it satisfies the spec: `tools/publish-nuget.ps1` writes a structured `publish-summary.json` (pushed/failed/notAttempted), emits a `PARTIAL PUBLISH - manual reconciliation required` GitHub Actions error annotation, and appends a Markdown step summary; `.github/workflows/release.yml` invokes `tools/create-partial-publish-issue.ps1` on workflow failure; the helper opens or comments on a `PARTIAL PUBLISH <version>` GitHub Issue with the run URL, status, package lists, and runbook reference. Coverage in `tests/tooling/publish_nuget/publish_nuget_test.py` exercises success, all-fail (`publish-failed`), middle-package-fail (`partial-publish`), pre-push validation failure, issue creation, issue commenting on rerun, and helper skip on `publish-failed` status. Operator recovery is explicit in `docs/dev/release-runbook.md` Failure And Recovery Notes (HTTP 409 vs non-409 distinction, rerun-and-self-heal contract).

## Deferred from: code review of story-12.1 (2026-04-30)

- **12.1-RV1 [resolved in 14.2]. Add SHA-256 / checksum evidence to release-runbook package table.** `docs/dev/release-runbook.md` now ships a `Per-Release Package Audit Evidence` subsection that requires SHA-256 capture for every future release alongside the Package Evidence table, with deterministic Windows pwsh (`Get-FileHash -Algorithm SHA256`) and Linux (`sha256sum`) commands and explicit equivalents (`dotnet nuget verify --all`, `nuget verify -Signatures`) for signature-based provenance. The historical `v1.2.0` block remains as-is because the source CI artifacts are no longer available locally; the requirement applies to releases after Story 14.2.
- **12.1-RV2 [resolved in 14.2]. Pin "semantic-release-bot" display name to a concrete GitHub App / user identity.** `docs/dev/release-runbook.md` adds a `Release Identity And Forensic Anchors` section that pins the GitHub Actions GitHub App (App ID `41898282`, posts as `github-actions[bot]`) as the canonical token identity for tag, GitHub Release, and package-asset writes; lists the four anchors reviewers should capture per release (Actions run URL, tag commit SHA + tagger identity, Release "Created by", trigger event); and treats anything else as a forensic red flag.
- **12.1-RV3. Document edge case where PR-merge commit body contains `[skip ci]` substring.** `release.yml`'s skip-CI guard checks `head_commit.message` for the substring. A merge commit whose squash body legitimately contains `[skip ci]` (quoting another commit, copying changelog text) would silently suppress the release. Branch protection now blocks direct pushes, so the only producer of merge commits is PRs. Re-open trigger: first observed silently-skipped release. (`.github/workflows/release.yml:18`)
- **12.1-RV4. Verify `package-lock.json` is tracked in git.** Sprint-status comment dated 2026-04-26 (Epic 11 closeout, P1) flagged the file as in working tree but untracked. `v1.2.0` shipped successfully on 2026-04-30, which implies `npm ci` worked, but neither this story's runbook nor the Dev Agent Record confirms `package-lock.json` is committed. Re-open trigger: first `npm ci` failure on a fresh clone, or sweep of Epic 11 leftover P-items. (`package-lock.json`)
- **12.1-RV5. Add `CONTRIBUTING.md` cross-link to the new release runbook.** File Scope explicitly permits an `UPDATE only for a cross-link to the runbook`; spec intent treats the runbook as the new operational source of truth. Adds discoverability without scope creep into Story 12.2. (`CONTRIBUTING.md`)
- **12.1-RV6 [resolved in 12.1].** Cross-reference of S11-FA / Story 12.6 from release runbook recovery notes was applied as a patch during the 12.1 code-review pass instead of being deferred. Operator-context only; the resolution of S11-FA itself remains tracked under S11-FA and Story 12.6.

## Deferred from: code review of story-12.4 (2026-05-01)

- **12.4-RV1 [resolved in 14.1]. CI shallow `git fetch ... || true` swallows ALL fetch failures.** `.github/workflows/ci.yml:37` masks auth/network/repository-rename errors and silently degrades the story-scope diff to `git diff-tree -r HEAD` (every file in HEAD). Drop `|| true` so fetch failures are loud. Out of Story 12.4 file scope; should land in a CI-hardening story.
- **12.4-RV2 [resolved in 14.1]. CI uses 3-dot `git diff origin/main..."$head_sha"` with `--depth=1` shallow fetch.** `.github/workflows/ci.yml:39` — `A...B` requires a reachable merge-base, which depth=1 cannot guarantee. Either fetch enough history (`--depth=50` or `--unshallow`) or switch to 2-dot semantics. Out of Story 12.4 file scope.
- **12.4-RV3 [resolved in 14.1]. CI force-push fallback no-ops on first push to `main` itself.** `.github/workflows/ci.yml:36-46` — when `origin/main` after fetch equals `head_sha`, `git diff` returns empty and the validator silently passes. A direct push to main bypasses story-scope checks entirely. Branch protection should normally prevent this, but the workflow should fail loudly when the diff is empty under push-to-main.
- **12.4-RV4 [resolved in 14.1]. CI `BRANCH_NAME` heredoc uses fixed sentinel `__STORY_SCOPE_EOF__`.** `.github/workflows/ci.yml:51-55` — predictable delimiter that a hostile branch name could contain. Defense-in-depth; replace with a randomized sentinel.
- **12.4-RV5 [resolved in 14.1]. CI propagates empty / blank `branch_name` with unhelpful diagnostic.** `.github/workflows/ci.yml:23-27` — when both `PR_HEAD_REF` and `GITHUB_REF_NAME` are empty, downstream errors blame "no story key" instead of identifying the missing env. Hard-fail at the env-binding step.
- **12.4-RV6 [resolved in 14.5]. `baselineRelated` and `HasReleaseFilter` rely on substring heuristics over author-controlled prose.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:189-202` — tokens `baseline`, `test-release.ps1`, `release lane` drive classification. Schema-strengthen the deferred-work entry format (e.g., a `Filter:` line per entry) and parse structure rather than prose. Follow-up to the patches landed in this review pass.
- **12.4-RV7 [resolved in 14.1]. `--story-key` value with multiple keys silently picks the first match.** `tools/check-story-file-scope.py:170-178` — inconsistent with trailer multi-key rejection. Story 12.3 territory; reject loudly to mirror trailer behavior.
- **12.4-RV8 [resolved in 14.1]. Branch name with multiple keys silently picks the first match.** `tools/check-story-file-scope.py:183-185` — same asymmetry as 12.4-RV7. Story 12.3 territory.
- **12.4-RV9 [resolved in 14.1]. `STORY_KEY_PATTERN` lacks unit assertions for boundary cases.** `tools/check-story-file-scope.py:13-16` — single-letter third segment, trailing-hyphen rejection are not directly tested. Story 12.3 territory.
- **12.4-RV10. `extract_backtick_path` silently drops bare-token bullets without an author-facing diagnostic.** `tools/check-story-file-scope.py:204-212` — author who forgets backticks gets no warning. Story 12.3 author UX.
- **12.4-RV11. `to_posix(path)` embeds Windows drive letter in diagnostic header.** `tools/check-story-file-scope.py:347-348` — emit `story_path.relative_to(REPO_ROOT).as_posix()` instead. Story 12.3 territory; cosmetic.
- **12.4-RV12 [resolved in 14.1]. Code-fence toggle mis-parses fences of length > 3 with nested 3-backtick content.** `tools/check-story-file-scope.py:20,222-228` — Markdown's nested-fence form is supported by parsers but breaks the toggle. Story 12.3 territory.
- **12.4-RV13 [resolved in 14.1]. `ALLOWED_LABELS` trailing-`:` heuristic truncates allow-list on legitimate trailing-colon prose.** `tools/check-story-file-scope.py:243-247` — only known section markers should terminate the allow-list. Story 12.3 territory.
- **12.4-RV14 [resolved in 14.1]. `git interpret-trailers` not on PATH crashes the validator with raw `FileNotFoundError`.** `tools/check-story-file-scope.py:133-141` — emit a clean `ValidationError` with actionable message. Story 12.3 territory.
- **12.4-RV15 [resolved in 14.1]. `section_block` test helper trims blank lines as section terminators.** `tests/tooling/story_scope/story_scope_validator_test.py:1108-1120` — could mask future validator regressions where Out-of-scope sections gain a blank-line continuation. Test-helper hardening.
- **12.4-RV16 [resolved in 14.1]. `test_branch_and_trailer_agreement_passes` lacks `assertNotIn("Conflicting", ...)` negative assertion.** `tests/tooling/story_scope/story_scope_validator_test.py:1196-1199` — passes today but would silently co-exist with a future conflict-detection regression that exits 0. Test hardening.
- **12.4-RV17 [resolved in 14.1]. `test_unparseable_explicit_story_key_fails_closed` couples to stdout sink.** `tests/tooling/story_scope/story_scope_validator_test.py:1324-1334` — `assertIn` only checks stdout; if the error path moves to stderr, the test silently breaks. Test hardening.
- **12.4-RV18 [resolved in 14.1]. Fixture-based scope tests do not assert which story file was loaded.** `tests/tooling/story_scope/story_scope_validator_test.py:1426-1456` — a future loader-precedence bug could silently load a different file. Test hardening.
- **12.4-RV19 [resolved in 14.5]. `DeferredKeyRegex` format brittleness — uppercase `S11-F[A-Z0-9]+\.` with literal trailing dot only.** `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs:1041` — em-dash, colon, or lowercase variants are silently ignored. Today all S11-F* entries use the literal-period format, so future-resilience only. Re-open trigger: first deferred-work format change.
- **12.4-RV20. AC #1 strict literal per-SHA replay drill.** Story 12.4 satisfied AC #1 via HEAD-replay coverage (HEAD strictly includes Epic 8.x SHA `d7495a3`, Epic 9.x SHA `bc4d5cc`, and Epic 10.x SHA `8207b54` in its ancestry, and the surviving test inventory at HEAD is a superset of those completion states — see Story 12.4 Decision Resolutions D3). A literal interpretation of AC #1 would also exercise each anchor SHA via `git checkout`, restore, build, and run both authoritative lanes against that exact tree. Re-open trigger: a release post-mortem that traces a regression to a test that existed at one of the named SHAs and was silently fixed before HEAD; or a future quality-discipline story that prefers strict literal AC #1 evidence over inheritance argumentation.

## Closed by: Story 14.1 CI Story-Scope Enforcement Hardening (2026-05-04)

Story 14.1 took ownership of the CI story-scope, validator, and test-hardening findings deferred
from the Story 12.3 and 12.4 review passes. Each closure below names the change that closes it and
where the evidence lives.

- **12.4-RV1 — closed.** `.github/workflows/ci.yml` no longer wraps `git fetch ... origin main` with
  `|| true` and no longer falls back to `git diff-tree -r HEAD`. Fetch failures exit 1 with a
  `::error::story-file-scope:` diagnostic that names the failed operation.
- **12.4-RV2 — closed.** The push-fallback path now resolves an explicit `base_sha=$(git rev-parse
  origin/main)` and runs a 2-dot `git diff --name-only "$base_sha" "$head_sha"`, on top of
  `actions/checkout@v6`'s `fetch-depth: 0` clone. No more 3-dot reachability against shallow history.
- **12.4-RV3 — closed.** When `origin/main` resolves to the same commit as `head_sha`, the job
  exits 1 with a direct-push / empty-diff diagnostic instead of silently passing file-scope
  validation.
- **12.4-RV4 — closed.** `BRANCH_NAME` heredoc delimiter is randomized per run as
  `STORY_SCOPE_EOF_$(date +%s%N)_${$}_${RANDOM}_${RANDOM}` so a hostile branch name cannot collide
  with the closer.
- **12.4-RV5 — closed.** Empty `branch_name` (and empty `head_sha` / `base_sha`) hard-fails the
  job at the env-binding step with a diagnostic that names the missing variable; "no story key
  resolved" no longer hides a missing-env case.
- **12.4-RV7 — closed.** `--story-key` rejects values containing more than one story key and lists
  every detected key. Test: `test_multiple_keys_in_explicit_story_key_value_fails_with_all_keys_reported`.
- **12.4-RV8 — closed.** Branch-name parsing rejects branches whose value contains more than one
  distinct story key (separated by a non-`[\w-]` character such as `/`) and lists every detected
  key. Test: `test_multiple_keys_in_branch_name_fails_with_all_keys_reported`.
- **12.4-RV9 — closed.** Added `STORY_KEY_PATTERN` boundary tests for trailing-hyphen rejection,
  uppercase normalization, and single-letter title segment.
- **12.4-RV12 — closed.** `parse_allowed_scope` tracks the open fence's marker character and
  length so fences longer than three backticks containing nested 3-backtick fences (and tilde
  fences containing nested backtick fences) both parse correctly. Tests:
  `test_parser_handles_fences_longer_than_three_backticks`,
  `test_parser_handles_tilde_fence_with_nested_backtick_fence`.
- **12.4-RV13 — closed.** Allow-list collection terminates only on known section labels
  (`Read/verify only:`, `Forbidden by default:`, including their `**bold:**` variants) or `## `
  headings; bullets whose rationale ends with `:` are no longer dropped. Tests:
  `test_parser_does_not_terminate_on_bullet_with_trailing_colon_rationale`,
  `test_parser_terminates_on_known_section_label_only`,
  `test_parser_does_not_terminate_on_unrecognized_prose_with_trailing_colon`.
- **12.4-RV14 — closed.** `subprocess.run(["git", ...])` calls in `parse_trailers` and `run_git`
  catch `FileNotFoundError` and raise a clean `ValidationError` naming `git interpret-trailers`
  with an install / `PATH` hint. No Python traceback reaches contributors. Test:
  `test_missing_git_interpret_trailers_reports_clean_validation_error`.
- **12.4-RV15 — closed.** `section_block` helper no longer terminates on blank lines; only
  non-blank, non-bullet lines end a section.
- **12.4-RV16 — closed.** `test_branch_and_trailer_agreement_passes` asserts the diagnostic does
  NOT contain `Conflicting story keys`, so a future regression that exits 0 while emitting the
  conflict diagnostic cannot silently co-exist with the test.
- **12.4-RV17 — closed.** `test_unparseable_explicit_story_key_fails_closed` matches against
  combined stdout + stderr via the `stdio()` helper, so the test does not break silently if the
  error path moves between sinks.
- **12.4-RV18 — closed.** `test_fixture_test_reports_loaded_story_artifact_path` pins the full
  `Story artifact:` line under the fixture artifacts root, so a future loader-precedence bug that
  loads a different file fails loudly.
- **12.3-RV15 — closed.** Multi-block `Allowed files for this story:` parsing is now exercised by
  `test_parser_merges_multiple_allowed_files_blocks`. The validator merges entries across blocks
  consistently; future shape drift fails the test instead of changing scope silently.

The Story 12.4 entries below are intentionally NOT closed by 14.1; they are carried forward with
refreshed rationale and a re-open trigger:

- **12.4-RV6** — out of 14.1 scope (CI test inventory parser, not the story-scope lane).
- **12.4-RV10** — the existing `Out-of-scope files:` diagnostic surfaces dropped bare-token bullets
  whenever a contributor's changed-file landing references one. A separate parse-time stderr
  warning would help story authors before any commit, but adding it was not part of 14.1's ACs and
  risks noisy false positives on legitimate non-bullet prose. Re-open trigger: an author-confusion
  incident or a story template redesign that needs pre-commit author warnings.
- **12.4-RV11** — cosmetic only. CI uses a repo-relative `_bmad-output/implementation-artifacts`
  artifacts root, so production diagnostics never embed a drive letter; the issue surfaces only
  in local Windows runs. Re-open trigger: a maintainer-visible diagnostic that exposes a local
  Windows path in a maintainer-facing channel.
- **12.4-RV19** — out of 14.1 scope (deferred-work parser brittleness in `CiTestInventoryTests`).
- **12.4-RV20** — out of 14.1 scope (Story 12.4 strict-literal AC #1 evidence drill).


## Closed by: Story 14.2 Release Pipeline Audit Hardening (2026-05-04)

Story 14.2 took ownership of the release-workflow, package-validation, and release-evidence
findings deferred from the Story 11.1 + 11.2 review pass and the Story 12.1 review pass. Each
closure below names the change that closes it and where the evidence lives. Entries with an
inline `[resolved in 14.2]` marker above are also listed here for the per-story rollup.

- **W1 — closed.** `.github/workflows/release.yml` pins `actions/checkout`, `actions/setup-dotnet`,
  `actions/setup-node`, `actions/cache`, and `actions/upload-artifact` to 40-char commit SHAs with
  trailing `# v<x.y.z>` comments. `CiTestInventoryTests.ReleaseWorkflow_ThirdPartyActions_ArePinnedToCommitSha`
  enforces the SHA shape so a future revert to a floating major-tag fails the test.
- **W2 — closed.** `tools/validate-release-packages.ps1` iterates every `src/**/*.csproj`, requires
  an explicit `<IsPackable>true|false</IsPackable>` declaration, asserts the project appears in
  exactly one inventory bucket, and rejects missing/blank/unsupported `IsPackable` values. New
  Python fixture suite at `tests/tooling/release_packages/release_packages_test.py` covers
  missing/unexpected/duplicate inventory entries, both bucket misuses, and the three IsPackable
  failure modes via temporary sentinel csproj files under `src/`.
- **W12 — closed.** `tools/release-packages.schema.json` defines required keys with
  `additionalProperties: false`, `pattern` constraints on IDs and project paths, and `uniqueItems`.
  `tools/release-packages.json` now references the schema via `$schema`, and the validator runs
  `Test-Json -SchemaFile` before any structural use, so misspellings such as `packageID`,
  `projectPath`, or `nonPackableProject` fail loudly before pack/publish scripts run.
- **W15 — closed.** `validate-release-packages.ps1` normalizes `-Version 1.2.3+local` to the
  NuGet-comparable `1.2.3` via `ConvertTo-NormalizedNuGetVersion`, emits a `Note:` diagnostic
  naming both forms, and threads the normalized value through both per-package and internal
  cross-package dependency-version assertions. `pack-release.ps1` is unchanged because
  semantic-release passes versions without build metadata in the CI path.
- **W19 — closed.** `concurrency: cancel-in-progress: false` is preserved deliberately to keep
  rerun-and-self-heal partial-publish recovery viable (`tools/publish-nuget.ps1 --skip-duplicate`).
  An inline comment in `release.yml` documents the trade-off and
  `CiTestInventoryTests.ReleaseWorkflow_Concurrency_PreservesPartialPublishSelfHeal` enforces it.
- **S11-FD — closed.** Existing structured `publish-summary.json`, `PARTIAL PUBLISH` annotation,
  step-summary, and `tools/create-partial-publish-issue.ps1` issue/comment path were audited as
  sufficient. `tests/tooling/publish_nuget/publish_nuget_test.py` exercises success, all-fail,
  middle-package-fail, pre-push validation failure, issue creation, issue commenting on rerun,
  and helper skip on `publish-failed` status. Operator recovery (HTTP 409 vs non-409, rerun
  contract) is explicit in `docs/dev/release-runbook.md` Failure And Recovery Notes.
- **12.1-RV1 — closed.** `docs/dev/release-runbook.md` Per-Release Package Audit Evidence
  subsection now requires SHA-256 capture for every release with deterministic Windows pwsh
  (`Get-FileHash -Algorithm SHA256`) and Linux (`sha256sum`) commands, plus
  `dotnet nuget verify --all` and `nuget verify -Signatures` as audit-equivalent options for
  signature-based provenance. Historical `v1.2.0` is not retroactively backfilled.
- **12.1-RV2 — closed.** `docs/dev/release-runbook.md` Release Identity And Forensic Anchors
  section pins the GitHub Actions GitHub App (App ID `41898282`, posts as `github-actions[bot]`)
  as the canonical token identity and lists the four anchors reviewers must capture per release.

The Story 11.1 + 11.2 deferred entries below are intentionally NOT closed by 14.2; they are
carried forward with refreshed rationale and a re-open trigger:

- **W3..W11, W13, W14, W17, W18, W20..W24** — out of 14.2 scope (CI workflow, test infra, and
  contracts/CLI hardening unrelated to the release-lane audit). 14.2 limits its file scope to
  release-pipeline artifacts.
- **W16 — partially closed.** Cleaned up in `tools/validate-release-packages.ps1`. The mirror in
  `tools/publish-nuget.ps1` is intentionally not touched in 14.2 because the story's file scope
  only permits a `publish-nuget.ps1` edit when there is a concrete partial-publish gap; cosmetic
  alignment alone does not meet that bar.
- **S11-FB** — out of 14.2 scope (compile-time symbol verification for integration-fast surfaces).
- **S11-FC — carried forward with fresh defer-by 2026-08-04.** Stale-tag preflight still requires
  either an `npx semantic-release --dry-run` cost on every release or carrying our own
  version-computation logic. Story 14.2 reassessed and confirmed neither option meets the
  cost/benefit bar without an observed collision; refreshed the re-open trigger and defer-by date.

The Story 12.1 deferred entries `12.1-RV3`, `12.1-RV4`, and `12.1-RV5` were intentionally left out
of 14.2 scope per the story file scope, even though they are adjacent to the runbook edits. They
remain open with their original re-open triggers.


## Deferred from: code review of 13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide (2026-05-03)

- **13.7-RV1. End-to-end uses Redis `KEYS` in 3-minute polling loop.** `tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:192` — `KEYS` is O(n) and prod-banned but acceptable in tests with bounded data and a non-parallel collection; the 3-min budget masks slow CI. Re-open trigger: any flake report attributing to this wait, or a need to scale the integration suite.
- **13.7-RV2. URL-escape `tenantId`/`canary` in search query interpolation.** `tests/Hexalith.Memories.IntegrationTests/Ingestion/OllamaEmbeddingEndToEndTests.cs:107` — defensive only; both values are `Guid.NewGuid().ToString("N")` hex without URL-reserved characters. Re-open trigger: generator change that introduces non-hex chars.
- **13.7-RV3. Clean up parent temp directory in `DeleteTempDaprConfig`.** `tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs:474-487` — only `config.yaml` is removed; AppHost-generated component yamls accumulate per random `daprAppId` under `%TEMP%/hexalith-memories-dapr/`. Low impact; cleanup is feasible if the AppHost-generated files are also enumerated. Re-open trigger: CI temp-space exhaustion or first complaint.
- **13.7-RV4 [resolved 2026-05-12]. Consolidate duplicate `ResolveRepositoryRoot` helpers.** `tests/.../AspireIngestionPipelineFixture.cs:489-501` and `src/Hexalith.Memories.AppHost/Program.cs` — same concept, two implementations, brittle five-`..` magic count in the fixture fallback. Resolved by the AppHost-owned `RepositoryRootLocator` shared by AppHost startup and the Aspire integration fixture.
- **13.7-RV5 [resolved in 14.5]. Truncate or rewrite `sprint-status.yaml` history comment lines.** `_bmad-output/implementation-artifacts/sprint-status.yaml` — entries accumulate per-event comment blurbs into multi-thousand-character logical lines (existing 13-2..13-7 entries all exhibit this). Project-wide pattern; coordinated convention change required. Re-open trigger: a parser/tool that fails on the long lines, or readability complaint.
- **13.7-RV6. Add dedicated `[Fact]` cases for AC4 malformed-token-form rejection branches.** `tests/.../OllamaOidcFakeServerTests.cs` — fake rejects missing `Content-Type`, missing `grant_type`, missing `client_id`, missing `client_secret`, and malformed bodies at runtime, but no `[Theory]+[InlineData]` enumerates each branch. Coverage gap rather than behavior gap; AC4 spirit met via wrong-path test plus runtime guards. Re-open trigger: a regression where the fake's rejection logic was weakened without tests catching it.
- **13.7-RV7. Replace `EmbedRequestCount.ShouldBeGreaterThanOrEqualTo(2)` magic number.** `tests/.../OllamaEmbeddingEndToEndTests.cs:116` — the rationale (raw + NL embeddings = 2 calls) is implicit. A named constant or comment would prevent brittleness if production legitimately changes the call count. Re-open trigger: assertion fails after a refactor and the cause is not immediately obvious.


## Deferred from: code review of 15-3-live-migration-coordination-policy (2026-05-14)

- **15.3-RV6. `GenerateEmbeddingActivity._redis is not null` silent no-op when keyed Redis service is missing.** `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:81-91` — File Scope L88 wording "if the runtime marker reader can be injected cleanly" treats this guard as intentionally optional; the mandatory correctness gate is at both indexing activities. Follow-up: either make `IConnectionMultiplexer` required at this site or emit a startup warning when the keyed registration is absent. Re-open trigger: any production deployment where DI omits the `"redis"` keyed `IConnectionMultiplexer` registration but the indexing activities also become optional, or a missed-write incident attributed to this site.
- **15.3-RV8. `WaitAsync(ct)` cancels the await but not the underlying Redis command.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:49`, `RedisEmbeddingMigrationStore.cs:181,198-199,222-223` — repo-wide pattern; cancelling the await leaks pending Redis work but does not stop the call. Re-open trigger: a Redis connection-exhaustion or pile-up incident traced to migration marker read/write paths.
- **15.3-RV10. `CompleteMigrationMarkerAsync` leaves stale `targetProvider/Model/Dimensions` on the active-marker key after `status=completed`.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:217-223` — reader short-circuits on `status == completed` so no functional issue today; debugging hygiene only. Re-open trigger: a future code path reading the active-marker hash without checking `status` first, or an operator complaint that completed markers show contradictory target metadata.
- **15.3-RV13. `OrdinalIgnoreCase` provider/model comparison vs case-sensitive downstream Redis hash keys.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:92-93` — requires a broader audit of downstream key generation across raw/NL/migration paths to confirm whether a case-distinct write that passes the guard can still produce mixed-metadata Redis state. Re-open trigger: an incident where a tenant ends up with mixed-case provider/model metadata after migration.
- **15.3-RV15. `StartMigrationMarkerAsync` does not detect an existing active marker pointing to a different target on the same tenant.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:172-200` — a fresh non-resume start for target B silently overwrites the active marker while a B different target A migration may still be in progress. Out of story 15.3 scope. Re-open trigger: an operator-coordination incident where two migrations are launched concurrently on the same tenant.
- **15.3-RV16. `CompleteMigrationMarkerAsync` does not verify the active marker target matches the completing target.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:211-223` — same root cause as 15.3-RV15: completion writes `status=completed` to the active-marker key regardless of which migration is completing. Re-open trigger: same as 15.3-RV15.
- **15.3-RV18. Active-marker hash has no TTL; orphaned markers block tenant ingestion until manual cleanup.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:198-199` — spec explicitly says marker is retained until clean completion. Operator alerting / manual-clearance command is a follow-up. Re-open trigger: an operator escalation where a crashed migration left a tenant blocked with no automated alert.
- **15.3-RV22. `13.6-RV2` swept in based on "Story 15.3 touched the file substantively, gained copyright header" rationale.** `_bmad-output/implementation-artifacts/deferred-work.md:210-219` — borderline-compliant with the spec's "records why they became in scope" clause; rationale could be sharper about *why* file-touch resolves the deferred risk. Re-open trigger: another `13.6-RV*` ID is closed with the same weak rationale and triggers a governance question.
- **15.3-RV24. Story status moved `ready-for-dev` → `review` without an `in-progress` step.** `sprint-status.yaml` — process flag only, not code; create-story → dev-story workflow could record an explicit `in-progress` transition. Re-open trigger: any tooling that breaks on missing `in-progress` history.
- **15.3-RV25. Operator-docs downtime statement could be sharper about per-tenant retry disruption.** `docs/operations/embedding-providers.md` — "Tenant-specific ingestion downtime is not required" is correct but some readers will interpret operator-visible per-tenant retry as "effective downtime"; phrasing could be tightened. Re-open trigger: operator confusion or escalation citing the downtime statement.
- **15.3-RV26. `HashEntry` integer value culture-dependent parsing is a future-regression risk.** `src/Hexalith.Memories.Server/Migration/EmbeddingMigrationMarkerReader.cs:64-65` — current path stores `int` via `HashEntry(string, int)` overload which is invariant; a future refactor to a string overload could silently regress to locale-sensitive parsing → fail-open. Re-open trigger: any refactor of the marker write path away from the `int`-typed `HashEntry` overload.
- **15.3-RV27. Stale per-target marker can resume against drifted state.** `src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs:179-187` — `--resume` only checks per-target key existence; does not verify active marker still references same target. Overlaps 15.3-RV15/16. Re-open trigger: same as 15.3-RV15.

## Deferred from: code review of 1-1-project-scaffolding-and-single-command-boot (2026-05-16)

Fresh re-review of Story 1.1 scaffolding files at HEAD `76aa84c` surfaced seven items deferred as pre-existing, intentional, or low-value. Two decision-needed items and thirteen unresolved patches remain in the story file for action.

- **1.1-RR1. Process-wide environment mutation when wiring DAPR API tokens.** `ApplyProcessEnvironmentTokens` sets `APP_API_TOKEN`/`DAPR_API_TOKEN` on the AppHost process so spawned daprd sidecars inherit them, but the variables persist for every child process the AppHost spawns afterwards.

  - ID: 1.1-RR1
  - Status: accepted
  - Source story: 1-1-project-scaffolding-and-single-command-boot
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs
  - Re-open trigger: A child-process leak surfaces in audit (token visible in unrelated process env), or CommunityToolkit.Aspire.Hosting.Dapr exposes a per-sidecar env API that replaces the global mutation.
  - Rationale: CommunityToolkit.Aspire.Hosting.Dapr 9.7 has no sidecar-specific env-builder API; the documented workaround is process env inheritance, and the AppHost only runs in development/CI/staging where the surface area is tightly scoped.

- **1.1-RR2. `DAPR_API_TOKEN_MODE` default silently disables token authentication.** A missing or typo'd `DAPR_API_TOKEN_MODE` value yields `(null, null)` from `ResolveDaprApiTokens` and skips both sidecar and application token wiring with no log entry.

  - ID: 1.1-RR2
  - Status: accepted
  - Source story: 1-1-project-scaffolding-and-single-command-boot
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs
  - Re-open trigger: A production incident traces a missing-token deployment back to a `DAPR_API_TOKEN_MODE` typo or omission.
  - Rationale: Default-disabled is the intentional posture for local dev and the Aspire integration-test fixture; production runs ship `DAPR_API_TOKEN_MODE=enabled` via secret manifest and never go through this branch silently.

- **1.1-RR3. Obsolete `WithReference` (CS0618) suppression hides upstream Aspire migration.** `#pragma warning disable CS0618` wraps the project-level component references; Aspire 14.x will remove the API.

  - ID: 1.1-RR3
  - Status: carried-forward
  - Source story: 1-1-project-scaffolding-and-single-command-boot
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs; Directory.Packages.props
  - Re-open trigger: Aspire 14.x package bump turns the warning into an error, or CommunityToolkit.Aspire.Hosting.Dapr releases a non-obsolete component-binding API.
  - Rationale: CommunityToolkit.Aspire.Hosting.Dapr 9.7 still reads project-level component references; removing the suppression now would break sidecar wiring with no upstream replacement. Owner: the AppHost/release maintainer carries the CS0618 suppression and removes it when the trigger fires (the Aspire 14.x bump, or a non-obsolete CommunityToolkit.Aspire.Hosting.Dapr component-binding API). Re-confirmed carried-forward by Story 19.1 (2026-06-30).

- **1.1-RR4. `RepositoryRootLocator.Resolve()` failure is unhandled in AppHost helpers.** `EnsureTestDataRoot`, `EnsureSecretsFile`, `ResolveDaprConfigPath`, and `ResolveRedisConfigPath` propagate raw `InvalidOperationException` if the AppHost runs from outside a recognizable repo layout.

  - ID: 1.1-RR4
  - Status: accepted
  - Source story: 1-1-project-scaffolding-and-single-command-boot
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs
  - Re-open trigger: A user runs the AppHost from a packaged distribution or detached workspace and files an issue about the cryptic "repository root not found" error.
  - Rationale: AppHost is dev/CI-side only and always launched from within the repo today; the locator's own exception message names the lookup keys and is debuggable.

- **1.1-RR5. `test-data/README.md` write race between parallel AppHosts.** `EnsureTestDataRoot` uses non-atomic `File.Exists` then `File.WriteAllText`; two simultaneous AppHost runs can collide on the README.

  - ID: 1.1-RR5
  - Status: accepted
  - Source story: 1-1-project-scaffolding-and-single-command-boot
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs
  - Re-open trigger: A developer reports a transient `IOException: file in use` on AppHost startup, or CI begins running multiple parallel AppHosts in a single sandbox.
  - Rationale: The README is created once per workspace lifetime; subsequent AppHost runs short-circuit on `File.Exists`. Collision window is sub-millisecond and only on the first ever run.

- **1.1-RR6. `AddJsonConsole` plus OTEL logger create dual log sinks.** ServiceDefaults registers OpenTelemetry logging (with scopes + formatted message) and `AddJsonConsole` simultaneously, producing two log records per emission when the OTLP exporter is also active.

  - ID: 1.1-RR6
  - Status: accepted
  - Source story: 1-1-project-scaffolding-and-single-command-boot
  - Target artifact: src/Hexalith.Memories.ServiceDefaults/Extensions.cs
  - Re-open trigger: A log-volume budget overrun in production is traced to dual-sink output, or downstream log shipping fails because of duplicated records.
  - Rationale: AC #3 explicitly calls for structured JSON logging via ServiceDefaults; the JSON console is the local-dev/Aspire dashboard surface, the OTEL exporter is the production sink. Both running side-by-side is the intended design for visibility parity.

- **1.1-RR7. `ResolveAllocatedEndpoint` `Single()` failure lacks context.** A missing or duplicated endpoint name surfaces as a bare `InvalidOperationException` with no message naming the resource or endpoint.

  - ID: 1.1-RR7
  - Status: accepted
  - Source story: 1-1-project-scaffolding-and-single-command-boot
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs
  - Re-open trigger: An Aspire upgrade renames endpoint keys and the unmatched lookup produces a triage ticket that costs more than the wrapping cost would have saved.
  - Rationale: AppHost-internal helper used for two known endpoint names (`redis`, `falkordb`); the surrounding `OnResourceReady`/`BeforeResourceStartedEvent` callbacks would themselves fail in a debuggable way if the endpoint contract drifts.

## Deferred from: code review of 15-6-scaffolding-hardening-sweep (2026-05-18)

Fresh three-layer review of the Story 15.6 scaffolding hardening sweep surfaced four items deferred as pre-existing, low-risk, or needing runtime verification rather than a blind patch.

- **15.6-CR1. Tight Redis PING reconnect loop without exponential backoff.** `WaitForRedisPingAsync` reconnects every 500 ms for up to 2 minutes against a Redis that may already be struggling; no backoff, no jitter.

  - ID: 15.6-CR1
  - Status: accepted
  - Source story: 15-6-scaffolding-hardening-sweep
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs
  - Re-open trigger: A developer reports AppHost log spam or a Redis backpressure incident traces back to the readiness probe loop.
  - Rationale: Cosmetic vs functional under current single-developer / CI load profile; the 500 ms cadence is below SE.Redis's own retry intervals and never has more than one in-flight connection.

- **15.6-CR2. Submodule guard `.git`-existence check does not detect partially-cloned submodules.** `Exists('{path}/.git')` passes as long as a `.git` file or directory is present at that path; it does not verify `HEAD` validity or that `git submodule update --init` actually populated content.

  - ID: 15.6-CR2
  - Status: accepted
  - Source story: 15-6-scaffolding-hardening-sweep
  - Target artifact: Directory.Build.props
  - Re-open trigger: A developer reports a fresh clone that "passes the submodule guard" but actually has missing content, traced to a network failure mid `git submodule update`.
  - Rationale: Story 15.6 only expanded the *count* of checked submodules (Story 1.1's pre-existing guard pattern); tightening the predicate to verify `HEAD` validity is a separate, broader scope and would require shelling out to git.

- **15.6-CR3. `File.WriteAllText` on DAPR component files is not atomic.** AppHost writes component YAMLs via `File.WriteAllText` which truncates-then-writes; a hot-reload watcher could read a partial file.

  - ID: 15.6-CR3
  - Status: accepted
  - Source story: 15-6-scaffolding-hardening-sweep
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs
  - Re-open trigger: `DAPR_COMPONENT_RELOAD_INTERVAL` gets enabled for local dev, or a developer reports a transient "invalid YAML" sidecar error during AppHost restart.
  - Rationale: Per-PID directory isolation means no other daprd process watches the same path; the local daprd does not have hot-reload enabled by default. Switching to write-temp-then-rename is a standalone hardening, not a Story 15.6 regression.

- **15.6-CR4 - resolved.** `ResolveAllocatedEndpoint` is no longer called before awaiting the rewrite TCS in `BeforeResourceStartedEvent`. The endpoint lookup now occurs after `WaitForRedisComponentRewriteAsync`, so an early sidecar-start event gives Redis allocation and component-file rewrite a chance to complete before the lookup runs.

  - ID: 15.6-CR4
  - Status: resolved
  - Source story: deferred-work-implementation-2026-05-19
  - Target artifact: src/Hexalith.Memories.AppHost/Program.cs
  - Re-open trigger: `ResolveAllocatedEndpoint(redis.Resource, "redis")` moves back above the rewrite wait, an Aspire upgrade changes the `BeforeResourceStartedEvent`-vs-allocation contract, or the `AppHostComponentFileOrderingTests` behavioral guard reproduces an early InvalidOperationException.
  - Evidence: `src/Hexalith.Memories.AppHost/Program.cs` snapshots the rewrite task, awaits `WaitForRedisComponentRewriteAsync(...)`, then resolves the Redis endpoint before `WaitForRedisPingAsync(...)`; `tests/Hexalith.Memories.IntegrationTests/Fixtures/AppHostComponentFileOrderingTests.cs` carries the Docker/Aspire behavioral guard for the ordering invariant.

## Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-05-20)

> **Resolution update — 2026-06-24 (dev-story carry-over pass).** Decisions taken with Jerome and the
> spec-required test-coverage gaps closed before moving Story 2.7 to review. The original findings below
> are preserved for history; current status per item:
>
> - **Resolved (implemented 2026-06-24):**
>   - **2.7-CR1** — shared `EvidencePacketCanonicalFixtures` added to `Hexalith.Memories.TestHelpers`; contract, CLI, MCP, and server tests now assert against the same canonical JSON (spec's "compare each surface against shared canonical JSON" option, rather than relocating the internal fixtures).
>   - **2.7-CR2** — CLI tests now cover empty, degraded, token-budget, unauthorized, single-axis, and default-caveat packet states.
>   - **2.7-CR3** — table-driven sanitization tests across unauthorized / backend-failure / partial-degradation / token-budget / server-diagnostic categories (`EvidencePacketSanitizationTests`).
>   - **2.7-CR4** — tenant/case negative isolation tests (`EvidencePacketIsolationTests`), written under the CR9 trust-upstream decision: they pin that `packet.Scope` is always request-derived and never source-derived, and that cross-case/tenant-wide search is preserved.
>   - **2.7-CR5** — server-side mapper tests (`EvidencePacketServerMappingTests`) drive the real `SearchResponseMetadataApplier`.
>   - **2.7-CR18 / 2.7-CR26** — single-axis + hybrid MCP canonical parity, deterministic axis-evidence ordering, and a structural text-fallback assertion replacing the brittle substring match.
>   - **2.7-CR27** — CLI stub already supports `onSearch`; single-axis CLI coverage added.
>   - **2.7-CR10** — *decision: implement.* `search query` JSON error envelopes now carry an additive `evidencePacket` (via `EvidencePacketMapper.FromError`), threaded opt-in through the executor → error-writer pipeline. The shared `CliOutputEnvelope<T>` `data XOR error` invariant and ADR-7.2-001 field-ordering are preserved (packet appended after `error`, null-suppressed for every other command). Local pre-resolution input-validation errors keep the minimal envelope (no resolved scope).
> - **Closed by decision (no code change):**
>   - **2.7-CR9** — *decision: trust upstream.* The mapper does not reconcile `source.CaseId` against request scope; cross-scope consistency is the upstream/server boundary. The packet-scope-is-request-derived invariant is pinned by 2.7-CR4 tests.
>   - **2.7-CR11 + 2.7-CR15** — *decision: defer to a future server-side hardening story.* The contract already carries the carriers (`EvidencePacketScope.IsolationStatus`, `OmittedReason.{Redaction,Policy,Authorization}`); only the server must begin emitting the empty-vs-unauthorized / redaction signal. No mapper heuristic added.
> - **Still deferred (non-spec enhancements / design or schema changes):** 2.7-CR6, 2.7-CR7, 2.7-CR8, 2.7-CR12, 2.7-CR13, 2.7-CR14, 2.7-CR16, 2.7-CR17, 2.7-CR19, 2.7-CR20, 2.7-CR21, 2.7-CR22, 2.7-CR23, 2.7-CR24, 2.7-CR25.

- **2.7-CR1. Canonical `EvidencePacketFixtures` not shared cross-surface.** Currently `internal static class` in `Hexalith.Memories.Contracts.Tests`; only consumed by `EvidencePacketSerializationTests`. Spec Task 5 demanded cross-surface fixture reuse (CLI, MCP, server tests). Rationale: requires moving fixtures to a shared test helper assembly (cross-cutting refactor) and re-keying CLI/MCP/server tests; paired with 2.7-CR2/CR3/CR4/CR5.
- **2.7-CR2. No CLI tests for empty/degraded/unauthorized/token-budget-compressed packets.** `EvidencePacketCliOutputTests.cs` has a single hybrid happy-path `[Fact]`. Spec Task 5 demanded full state coverage at the CLI surface. Rationale: significant test-scope expansion; depends on 2.7-CR1 shared fixtures.
- **2.7-CR3. No table-driven sanitization tests across the spec'd categories** (unauthorized, all-backend failure, partial degradation, token-budget compression, server diagnostics, MCP error mapping). Rationale: paired with 2.7-CR1 shared fixtures.
- **2.7-CR4. No tenant/case negative isolation fixtures.** Tests use only `tenant-a`/`case-a` happy-paths. Rationale: paired with decision on 2.7-CR9 (scope-consistency policy).
- **2.7-CR5. No server-side `EvidencePacketMapper` tests** (tenant/case scope, empty results, partial backend degradation, all-backend/unauthorized diagnostics, token-budget omitted metadata). Rationale: needs new test class scaffolding in `Hexalith.Memories.Server.Tests`.
- **2.7-CR6. MCP error path uses `UnknownScope()` for known-tenant errors.** `McpErrorMapper.cs:61, 86, 105, 132` — only the forbidden branch passes real scope. Rationale: requires plumbing `requestedTenantId`/`@case` through `Map`/`MapGeneric`/`MapValidation` from `SearchMemoryTool`; touches every MCP tool that hits the error mapper.
- **2.7-CR7. `MapOmittedReason` falls to default `None` for future enum values** (Density, Redaction, Policy, Authorization, TrueAbsence). Rationale: lower-level `OmittedReason` enum doesn't expose those today; reopens when the server starts emitting them.
- **2.7-CR8. SHA-256 expansion handle truncated to 16 hex (64 bits) + `|`-delimited material allows injection collisions.** `EvidencePacketMapper.cs:508-513`. Rationale: needs handle-format decision (length, delimiter, length-prefix vs delimiter).
- **2.7-CR9. `source.CaseId` copied verbatim from upstream with no scope-consistency check** (decision-needed). Rationale: needs design call between trust-upstream / overwrite / skip / throw.
- **2.7-CR10. CLI does not emit `EvidencePacket` on error responses** (decision-needed). Rationale: changes CLI JSON error envelope shape (`CliErrorWriter.WriteForCommand`); needs explicit decision about CLI error envelope schema, possibly affecting ADR-7.3-002.
- **2.7-CR11. Empty-vs-unauthorized discrimination cannot be made in the mapper without an upstream signal** (decision-needed). Rationale: needs a server-side change to expose authorization-driven emptiness; paired with 2.7-CR15.
- **2.7-CR12. `evidenceStrength: None` + `state: Complete` contradiction when best score is 0.** Rationale: needs precedence design between strength and state.
- **2.7-CR13. `EvidencePacketSource.Score` always serializes (required `double` source) — cannot represent "score unknown".** Rationale: lower-level `ScoredResult.Score` schema would need to become nullable.
- **2.7-CR14. Inconsistent `permissionsContext` values across surfaces** (`tenant`/`tenant-case`/`mcp-auth`/`mcp-error`). Rationale: needs a single source-of-truth constant list.
- **2.7-CR15. State precedence does not model `Redaction`/`Policy` states** (Party-Mode Hardening). Rationale: no upstream signal exists today; paired with 2.7-CR11.
- **2.7-CR16. `Combined` omission reason on degraded result strips token-budget hint from recovery.** Rationale: paired with 2.7-CR11/CR12 precedence redesign.
- **2.7-CR17. `McpErrorPayload` not registered in source-gen `MemoriesJsonContext`** (AOT-only risk; works today via reflection fallback). Rationale: needs Mcp-side source-gen context.
- **2.7-CR18. No single-axis MCP packet test, no default-caveat-fallback CLI test, no `AxisEvidence` determinism-ordering test, no CLI-stable-property-names story-assertion.** Rationale: paired with 2.7-CR1/CR2 coverage expansion.
- **2.7-CR19. `EvidencePacket` placed directly on lower-level `SearchResult`/`HybridSearchResult` records** instead of an envelope wrapper (design smell). Rationale: revert would require envelope wrapping at every consumer; mitigated by `[JsonIgnore(WhenWritingNull)]`.
- **2.7-CR20. `EvidencePacketResultSummary.Query` echoes raw caller query verbatim** (defense-in-depth length cap / sanitize). Rationale: caller-supplied, not a leak.
- **2.7-CR21. Whitespace-only `caseId` inconsistency** (`permissionsContext: "tenant"` while `scope.caseId: "   "`). Rationale: input-validation polish.
- **2.7-CR22. `ExpansionHandle.CaseId` JSON-ignored when null, `TenantId` always present (asymmetry).** Rationale: borderline scope-shape oracle; minor.
- **2.7-CR23. `HybridSearchResult.Results` not null-guarded in mapper.** Rationale: contract guarantees non-null via `required`; defensive-only.
- **2.7-CR24. `FromSearchResult` passes `null` for `AllEnabledAxesUnavailable` vs hybrid passing actual value.** Rationale: cosmetic; semantically correct since single-axis has no multi-axis concept.
- **2.7-CR25. `McpErrorMapper.MapAuthorization` forbidden message echoes `requestedTenantId`.** Rationale: caller-supplied input echoed back, not a confirmation of alternate tenant existence.
- **2.7-CR26. CLI test substring match for `"evidencePacket"` and serialization-test camelCase spot-check are brittle.** Rationale: paired with 2.7-CR1/CR2 coverage expansion.
- **2.7-CR27. `EvidencePacketCliOutputTests` stub `MemoriesClient` only overrides `HybridSearchAsync`.** Rationale: paired with 2.7-CR2 single-axis CLI coverage.

## Deferred from: code review of 16-1-projection-registry-cross-check-design (2026-05-20)

- **16.1-CR1. Whitespace-only entry in `SupportedEventTypePatterns` silently promoted to wildcard `*`** (`src/Hexalith.Memories.Server/Handlers/ProjectionBindingMatcher.cs:137-140`). Rationale: operator-error edge; promote to wildcard is consistent with empty-string semantics documented in `ProjectionBinding.SupportedEventTypePatterns` XML doc. Sweep later if any host reports surprise.
- **16.1-CR2. Trailing `.` or `/` in event names/source prefixes not trimmed before terminal-segment split** (`ProjectionBindingMatcher.cs:155-159, 183-187`). Rationale: not produced by current callers; defensive normalization deferred to a normalization sweep.
- **16.1-CR3. Embedded `\` in event names not normalized to `.` or `/`** (`ProjectionBindingMatcher.cs:135`). Rationale: serializers in this stack emit `.`-separated event types; backslash variant is theoretical.
- **16.1-CR4. Turkish-I / Unicode invariant casing produces non-byte-equal forms across tenants/routes** (`ProjectionBindingMatcher.cs:132, 167`). Rationale: current tenants/aggregates are ASCII; revisit when non-ASCII tenant ids are introduced.
- **16.1-CR5. Bare `V2` input yields empty event key → `tenant/source//` double-slash comparison key** (`ProjectionBindingMatcher.cs:161`). Rationale: cosmetic — comparison key is internal, only surfaces in Subject if a bare `V2` event reaches the detector; unlikely.
- **16.1-CR6. Tenant-leakage assertion absent on structured log/telemetry payload** (`tests/Hexalith.Memories.Server.Tests/Handlers/HandlerMismatchDetectorTests.cs`). Rationale: currently no foreign-tenant fields are emitted on the warning; defensive assertion to add when telemetry shape expands.
- **16.1-CR7. Multi-slash collapse (`while (... "//")`) has no direct test** (`ProjectionBindingMatcher.cs:127-130`). Rationale: covered transitively by slash-normalization tests; explicit regression test is low priority.
- **16.1-CR8. Wildcard suffix matching test depth — current tests pass via exact-match coincidence, not via `*` honoring** (`ProjectionBindingMatcher.cs:108-111`). Rationale: add a targeted test when wildcard semantics is widened.

## Deferred from: code review of 17-1-evidence-cockpit-and-trust-components (2026-05-20)

- **17.1-CR1. `EvidenceDisplay.Label` is locale-insensitive humanization that bypasses FrontComposer's `IStringLocalizer<FcShellResources>` pattern** (`src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs:11-13` and all display copy across the RCL). Rationale: broader localization work spans the whole new RCL, not just Evidence Cockpit; needs a Memories resource bundle decision before retrofitting.
- **17.1-CR2. `EvidencePacketScope.PermissionsContext` not surfaced anywhere in `MemoriesScopeHeader` or mapping** (`src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor`, `EvidencePacketViewMapping.cs`). Rationale: needs a UX call on where/how to display the machine-readable permission context (chip? expandable detail?); not blocking AC1.
- **17.1-CR3. `EvidencePacketOmittedDetails` body fields (`OmittedCount/FieldNames/DetailGroups/ExpansionHandles`) silently dropped beyond the binary token-budget indicator** (`MemoriesTrustStrip.razor`, `MemoriesEvidenceCockpit.razor`). Rationale: requires an "expand omitted details" UX (drawer? inline disclosure?) that is itself a separate story.
- **17.1-CR4. `EvidencePacketSource.AnnotationsCount/CaseId/CaseName` not rendered in Source Citation Stack** (`MemoriesSourceCitationStack.razor`). Rationale: deemed not load-bearing for AC3 inspection workflow; revisit when annotations or cross-case visibility surfaces are designed.
- **17.1-CR5. `EvidencePacketResultSummary.TotalCount/ReturnedCount/HasIndexedMemoryUnits` not surfaced** (`MemoriesEvidenceCockpit.razor:20-31`). Rationale: distinguishes empty-tenant from empty-result, but not strictly required by AC1; adds value when an Empty fixture lands.
- **17.1-CR6. `EvidencePacketEvidence.Degraded` / `AllEnabledAxesUnavailable` flags not fed into Trust Strip** (`MemoriesTrustStrip.razor`). Rationale: overlapping signal with `State`; defer until the precedence ladder is implemented and these flags can layer cleanly.
- **17.1-CR7. Task 6 accessibility checkboxes marked `[x]` despite no automated forced-colors / focus-return / touch-target / no-text-overlap check** (`_bmad-output/implementation-artifacts/17-1-evidence-cockpit-and-trust-components.md:90-95`). Rationale: Completion Notes correctly call out Playwright/axe deferred because no runnable web host was added in this RCL-only slice; refile a hardening story when a host (e.g., 17.5) lands.
- **17.1-CR8. `aria-label="Inspect source N"` exposes raw Rank including 0** (`MemoriesSourceCitationStack.razor:34`). Rationale: only relevant if Inspect button is reinstated with command wiring; the dead-button removal patch closes this path.
- **17.1-CR9. No negative tests for copy/export/MCP-inspect payload redaction parity** (`tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`). Rationale: Task 5 mandates these tests, but they are vacuous until command primitives exist; couples to dead-button removal patch.
- **17.1-CR10. No transition-state a11y coverage (loading→complete, complete→degraded, etc.)** (`tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`). Rationale: useful when actions trigger real state changes; trivial today since all states are rendered in isolation.
- **17.1-CR11. `<article>` + nested `<section aria-label>` creates a verbose landmark list under assistive tech** (`MemoriesEvidenceCockpit.razor:1-2` and children). Rationale: a11y refinement after primary leakage / precedence findings settle; not blocking AC2.
- **17.1-CR12. Source citation order test asserts `data-source-rank` attribute values rather than DOM iteration order** (`EvidenceCockpitTests.cs:104-106`). Rationale: acceptable proxy with the current rank-stable contract; tighten when contract permits rank duplication or absent ranks.
- **17.1-CR13. `SourceCountLabel` does not handle negative or `int.MaxValue` counts** (`EvidenceDisplay.cs:14-15`). Rationale: contract precludes negative counts; defensive-only.
- **17.1-CR14. `EvidencePacketSource.SourceType` and `axis.Axis` not wrapped in `SafeText`** (`MemoriesSourceCitationStack.razor:17`, `MemoriesRetrievalAxisBreakdown.razor:17`). Rationale: enum-like strings have controlled vocabulary; revisit if contract loosens to free-form strings.
- **17.1-CR15. Stale state never tested by fixture** — covered by the broader "5 of 8 states untested" patch above; standalone deferred.
- **17.1-CR16. CSS uses raw `flex-wrap: wrap` instead of `FluentStack` for Trust Strip layout** (`MemoriesEvidenceCockpit.razor.css`). Rationale: minor compliance gap with Fluent UI primitive preference; wrapping behavior itself is correct.
- **17.1-CR17. `Sources[].SourceUri` rendered without trust-mark badging (external URL vs local memory reference)** (`MemoriesSourceCitationStack.razor`). Rationale: not in AC3; needs a UX call on visual trust marks before implementing.
- **17.1-CR18. Graph path summary uses raw `<dl>/<dt>/<dd>` rather than `FluentDescriptionList`** (`MemoriesGraphPathSummary.razor`). Rationale: FrontComposer primitive preference; current markup is semantically correct.

### Triage: Story 17.1 deferred web items and architecture decisions (2026-07-06)

Correct-course trigger: Epic 17 retrospective action item 3 asked for `17.1-CR1` through `17.1-CR18` and the deferred web architecture decisions (command-palette scope, refresh persistence, mobile grid, role authorization model) to be moved into bounded follow-up stories or explicitly accepted here. This triage uses direct adjustment: completed Epic 17 history is not reopened, and no production backend/API scope is introduced.

Follow-up homes:

| Home | Items | Boundary |
|---|---|---|
| Story 25.7: Evidence Cockpit UX Conformance | `17.1-CR1`, `17.1-CR16`, `17.1-CR18` | Existing backlog story; keep it to FrontComposer/Fluent V5 conformance, localization, and mapper usage. |
| Future web story: Evidence Metadata and Trust Semantics Surfacing | `17.1-CR2`, `17.1-CR4`, `17.1-CR5`, `17.1-CR6`, `17.1-CR17` | Render already-contractual scope/source/summary/degradation fields after UX placement is approved; do not invent new evidence semantics. |
| Future web story: Omitted Details Expansion UX | `17.1-CR3` | Design and implement an explicit omitted-details disclosure/expansion pattern before exposing detail groups or handles. |
| Future web story: Command Surface Scope and Redaction Safety | `17.1-CR8`, `17.1-CR9`, `17-WEB-AD1-COMMAND-PALETTE-SCOPE` | Decide global/page/role command-palette scope and prove copy/export/MCP-inspect redaction parity before command actions are productized. |
| Story 17.7: Runnable Web Specimen and Browser/AT Accessibility Gap Closure | `17.1-CR7`, `17.1-CR10`, `17.1-CR11`, `17-WEB-AD3-MOBILE-GRID-STRATEGY` | Existing backlog story; closes browser, axe, forced-colors, reduced-motion, zoom/reflow, touch, mobile grid, focus, and screen-reader claims with evidence. |
| Future web story: Tenant-Scoped Refresh Persistence Policy | `17-WEB-AD2-REFRESH-PERSISTENCE` | Decide whether tenant/case/filter/source/grid state survives browser refresh, and prove stale-scope invalidation before implementation. |
| Accepted or resolved in this ledger | `17.1-CR12`, `17.1-CR13`, `17.1-CR14`, `17.1-CR15`, `17-WEB-AD4-ROLE-AUTHORIZATION-MODEL` | Defensive-only, already covered, or intentionally not a current product/security requirement. |

- ID: 17.1-CR1
  - Status: resolved
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs`; `src/Hexalith.Memories.Web/Resources/*`
  - Re-open trigger: any new Evidence Cockpit visible or assistive copy bypasses `EvidenceResourceKeys` and the EN/FR resource bundle.
  - Rationale: Story 25.7 routed cockpit headings, banners, enum labels, counts, freshness, timestamps, scores, captions, fallbacks, and accessible names through `IStringLocalizer<MemoriesWebResources>` with EN/FR parity tests.
  - Evidence: `EvidenceResourceKeys.cs`, localized Evidence components/helpers, and `EvidenceCockpitTests.Localization_EveryEvidenceKeyResolvesInEnglishAndFrench` plus the French multi-state rendering test.

- ID: 17.1-CR2
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesScopeHeader.razor`; `src/Hexalith.Memories.Web/Components/Evidence/EvidencePacketViewMapping.cs`
  - Re-open trigger: before the web surface claims to render the full Evidence Packet scope contract or adds a permissions-context chip/detail surface.
  - Rationale: `permissionsContext` placement is a UX information-architecture decision. It should be implemented with the other metadata surfacing work so scope, permission, and isolation signals stay coherent.

- ID: 17.1-CR3
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Omitted Details Expansion UX); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor`; `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor`
  - Re-open trigger: before omitted detail groups or expansion handles are exposed in any web cockpit, lens, copy/export, or command surface.
  - Rationale: Omitted detail expansion needs a deliberate disclosure pattern with token-budget, authorization, and backend-degradation semantics; adding ad hoc fields to the Trust Strip would make the state grammar harder to reason about.

- ID: 17.1-CR4
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor`
  - Re-open trigger: when annotations, cross-case source visibility, or case-name inspection becomes part of a selected web workflow.
  - Rationale: Source annotation and case metadata are valuable only when their placement is designed with trust marks and cross-case visibility; keep them in the metadata surfacing story rather than broadening Story 25.7.

- ID: 17.1-CR5
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor`
  - Re-open trigger: before the web surface ships an empty-tenant, empty-result, or indexed-memory-unit distinction to users.
  - Rationale: Result-summary counts affect user interpretation of empty and partial states. They belong with the metadata surfacing story so empty-result language, counts, and indexed-unit signals are tested together.

- ID: 17.1-CR6
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesTrustStrip.razor`
  - Re-open trigger: before the Trust Strip adds a precedence ladder for `Degraded` or `AllEnabledAxesUnavailable`, or any new surface claims all-axis availability status.
  - Rationale: These flags overlap with `State` and recovery semantics. They should be layered only after UX approves precedence and display rules.

- ID: 17.1-CR7
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `tests/Hexalith.Memories.Web.Tests/Components/Validation/*`
  - Re-open trigger: before any product-route accessibility claim, release note, or stakeholder acceptance says the web surface is validated beyond component-specimen bUnit evidence.
  - Rationale: Forced-colors, focus return, touch target, and no-overlap checks need a runnable host or specimen app. Component tests alone cannot close browser/AT validation.

- ID: 17.1-CR8
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor`
  - Re-open trigger: before an Inspect Source command/button is reintroduced or wired to command-palette activation.
  - Rationale: The dead-button removal closed the immediate path, but command wiring can reintroduce rank-label leakage. Keep the fix coupled to the command-surface story.

- ID: 17.1-CR9
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`
  - Re-open trigger: before copy, export, or MCP-inspect commands are exposed from the web cockpit or command palette.
  - Rationale: Redaction parity tests are vacuous until the relevant command primitives exist. They become mandatory acceptance evidence for the command-surface story.

- ID: 17.1-CR10
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`
  - Re-open trigger: when a runnable host/specimen app exists or web actions introduce real loading-to-complete, complete-to-degraded, or recovery transition paths.
  - Rationale: Transition-state accessibility is meaningful only when state changes happen in a running interaction surface; current isolated fixture rendering does not exercise those paths.

- ID: 17.1-CR11
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor`
  - Re-open trigger: during the first screen-reader pass or host-level landmark/heading audit for the web cockpit.
  - Rationale: Landmark verbosity is a real browser/assistive-technology concern. It should be fixed with screen-reader evidence rather than guessed from host-less markup.

- ID: 17.1-CR12
  - Status: accepted
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`
  - Re-open trigger: the Evidence Packet source-ranking contract allows duplicate ranks, absent ranks, or a separate user-visible order that can diverge from `data-source-rank`.
  - Rationale: The current rank-stable contract makes the attribute assertion an adequate proxy. Tightening to DOM-iteration assertions is defensive polish until the ordering contract changes.

- ID: 17.1-CR13
  - Status: accepted
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `src/Hexalith.Memories.Web/Components/Evidence/EvidenceDisplay.cs`
  - Re-open trigger: source counts become externally supplied, nullable, unbounded, or otherwise no longer contract-controlled non-negative integers.
  - Rationale: The contract precludes negative source counts. Adding defensive negative or `int.MaxValue` labels now would spend scope on an unreachable state.

- ID: 17.1-CR14
  - Status: accepted
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor`; `src/Hexalith.Memories.Web/Components/Evidence/MemoriesRetrievalAxisBreakdown.razor`
  - Re-open trigger: `SourceType` or axis values become provider-authored, user-authored, localized free text, or otherwise leave the controlled-vocabulary contract.
  - Rationale: Current values are enum-like controlled contract terms. Applying `SafeText` everywhere is acceptable future hardening, but not needed until the contract loosens.

- ID: 17.1-CR15
  - Status: resolved
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidencePacketFixtures.cs`; `tests/Hexalith.Memories.Web.Tests/Components/Evidence/EvidenceCockpitTests.cs`
  - Re-open trigger: stale-state fixture coverage is removed or a new stale-state rendering path bypasses the existing fixture and per-state assertions.
  - Evidence: Story 17.1 review patch added the `Stale` fixture as part of the broader empty/stale/degraded/partial/weak fixture expansion, and current tests still reference `EvidencePacketFixtures.StalePacket()` in Evidence Cockpit, recovery, filters, lenses, and responsive/accessibility suites.

- ID: 17.1-CR16
  - Status: resolved
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesEvidenceCockpit.razor.css`
  - Re-open trigger: Trust Strip wrapping is moved out of `FluentStack Wrap="true"` into hand-authored layout CSS.
  - Rationale: Story 25.7 keeps wrapping in the Fluent V5 stack primitive and verifies that the cockpit stylesheet contains no raw `flex-wrap` declaration.
  - Evidence: `MemoriesTrustStrip.razor` and `Epic17ConformanceRemediationTests.TrustStrip_UsesFluentStackWrappingInsteadOfRawFlexWrap`.

- ID: 17.1-CR17
  - Status: carried-forward
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Evidence Metadata and Trust Semantics Surfacing); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesSourceCitationStack.razor`
  - Re-open trigger: before source trust marks, external/local source distinction, or cross-case source cues are productized.
  - Rationale: Trust-mark badging needs a UX decision alongside source metadata and case visibility. Implementing it alone would risk adding a visual security claim without a shared legend.

- ID: 17.1-CR18
  - Status: accepted
  - Source story: 17-1-evidence-cockpit-and-trust-components
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 25.7 Evidence Cockpit UX Conformance); `src/Hexalith.Memories.Web/Components/Evidence/MemoriesGraphPathSummary.razor`
  - Re-open trigger: the pinned Fluent UI package or FrontComposer adds a description-list primitive.
  - Rationale: Story 25.7 verified that the pinned Fluent V5 assembly has no `FluentDescriptionList`; the semantic `<dl>/<dt>/<dd>` fallback remains explicitly allowlisted under owner Story 25.7.
  - Evidence: `Epic17ConformanceAllowlist` description-list entries and `Epic17ConformanceRemediationTests.GraphSummary_PinnedFluentPackageHasNoDescriptionListPrimitive`.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md`
  summary: Non-cockpit grid and lens callers still consume locale-insensitive `EvidenceDisplay` overloads.
  evidence: The reviewed cockpit and its Evidence child components use localized overloads, but `Components/Grid` and `Components/Lenses` retain pre-existing calls to the invariant enum, timestamp, freshness, and score formatters; localizing those mapper-driven surfaces requires a separate cross-RCL design rather than widening Story 25.7.

- ID: 17-WEB-AD1-COMMAND-PALETTE-SCOPE
  - Status: carried-forward
  - Source story: epic-17-retro-2026-06-24
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Command Surface Scope and Redaction Safety); `src/Hexalith.Memories.Web/Components/Interaction/*`
  - Re-open trigger: before the Memories web command palette becomes user-facing or a web action needs global, page-local, or role-density-scoped discovery.
  - Rationale: Command-palette scope changes user reachability, disabled reasons, tenant/case reset behavior, and redaction obligations. It is bounded with the command-surface story instead of patched inside a display component.

- ID: 17-WEB-AD2-REFRESH-PERSISTENCE
  - Status: carried-forward
  - Source story: epic-17-retro-2026-06-24
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (future web story: Tenant-Scoped Refresh Persistence Policy); `src/Hexalith.Memories.Web/Components/Interaction/*`
  - Re-open trigger: before preserving tenant, case, filter, selected packet/source, grid sort/page, or expanded evidence across browser refresh is claimed or implemented.
  - Rationale: Refresh persistence is not just convenience state; it can leak stale tenant/case context if it is not tenant-scoped and invalidated. It needs an explicit state-policy story before implementation; Story 17.7 can validate browser behavior but does not decide persistence semantics.

- ID: 17-WEB-AD3-MOBILE-GRID-STRATEGY
  - Status: carried-forward
  - Source story: epic-17-retro-2026-06-24
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 17.7 Runnable Web Specimen and Browser/AT Accessibility Gap Closure); `src/Hexalith.Memories.Web/Components/Lenses/*`; `src/Hexalith.Memories.Web/Components/Evidence/*`
  - Re-open trigger: before a mobile product surface claims data grids, timelines, or source lists remain usable without horizontal scrolling at phone/tablet widths.
  - Rationale: Mobile grid/card/timeline behavior needs real viewport evidence and trust-field preservation checks. Keep it tied to the runnable host validation story.

- ID: 17-WEB-AD4-ROLE-AUTHORIZATION-MODEL
  - Status: accepted
  - Source story: epic-17-retro-2026-06-24
  - Target artifact: `_bmad-output/implementation-artifacts/17-4-role-specific-web-inspection-lenses.md`; `docs/dev/adr-10.2-004-auth-granularity.md`; `_bmad-output/planning-artifacts/epics.md` (Epic 20 authorization evidence)
  - Re-open trigger: a product/security requirement introduces role-scoped web permissions, per-tool scopes, read-only agent access, or separate ingestion delegation.
  - Rationale: Current role-specific web lenses are evidence-density profiles over the same canonical Evidence Packet, not a permission model. The accepted auth model is authenticated caller plus matching tenant claim, with per-tool/role scopes deferred until a real consumer requires them. Adding role authorization now would invent policy outside the current product requirement.

## Parties Consumer Integration Intake (2026-05-27)

Cross-repository asks raised by the `Hexalith.Parties` consumer correct-course intake and carried forward into Epic 18 (Sprint Change Proposal 2026-05-27). Each entry maps an `MEM-n` ask to the Epic 18 story that closes its verified residual gap.

- ID: MEM-1
  - Status: carried-forward
  - Source story: parties-consumer-integration-intake-2026-05-27
  - Target artifact: docs/dev/public-surface-stability.md (review-enforced Mcp `PackageId` stability half); _bmad-output/implementation-artifacts/18-1-apphost-project-resolution-guard-and-public-surface-stability-contract.md
  - Re-open trigger: the published `Hexalith.Memories.Mcp` NuGet `PackageId` is renamed without a semantic-release `BREAKING CHANGE:` note, or a pack-time/analyzer guard becomes available to enforce the `PackageId` half that reflection cannot cover.
  - Rationale: Story 18.1 (done) delivered the compile-resolution guard test (`AppHostProjectResolutionTests`) and the name-stability contract (`docs/dev/public-surface-stability.md`), test-enforcing 5 of 6 contract items (project-symbol resolution, Server/Mcp assembly name + root namespace, Aspire symbol shape); the Mcp `PackageId` is a pack-time NuGet property not reflectable from a built assembly, so it stays review-enforced only and is carried forward as the residual half. Owner: AppHost/release maintainer. Story 19.1 (2026-06-30) refreshed this entry against the now-completed Story 18.1 without reopening Epic 18.
- ID: MEM-2
  - Status: resolved
  - Source story: parties-consumer-integration-intake-2026-05-27
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.2)
  - Re-open trigger: a downstream operator cannot fill deployment placeholders because the canonical env/port/OTLP config surface is undocumented or has drifted from code.
  - Evidence: Story 18.2 published the canonical deploy-config contract at `docs/operations/deployment-configuration.md` (OTLP env gate, Dapr sidecar ports, required runtime env, pub/sub event-intake surface, app-id reconciliation) and guards it against drift with `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs` (bidirectional doc<->code tie on the `EventIngestionController` constants plus authoritative source-file cross-checks). Residual full aspirate manifest emission is carried forward as `MEM-2-ASPIRATE`.
- ID: MEM-2-ASPIRATE
  - Status: accepted
  - Source story: 18-2-deployment-configuration-contract-publication
  - Target artifact: docs/operations/deployment-configuration.md (maintained deploy-config contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs; a future aspirate/Aspir8 manifest-emission story stays unassigned until the re-open trigger fires.
  - Re-open trigger: a downstream consumer needs ready-to-apply Kubernetes/Dapr manifests emitted from the AppHost topology rather than a hand-filled documented contract.
  - Rationale: Story 19.2 (2026-06-30) accepts the documented-contract approach as sufficient for current consumers and declines to schedule aspirate emission. The maintained deploy-config contract publishes every env/port/OTLP/pub-sub literal an operator must supply, and DeploymentConfigurationContractTests fails the build on doc<->code drift, so consumers fill kustomization placeholders today without generated manifests; no current consumer requires emitted manifests, and no aspirate tooling exists in src/** or tools/**. Per the 2026-05-27 "document now, defer aspirate" locked decision this is accept-until-trigger. Owner: AppHost / release maintainer.
- ID: MEM-3
  - Status: resolved
  - Source story: parties-consumer-integration-intake-2026-05-27
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.3)
  - Re-open trigger: an external Dapr ACL cannot be verified against the Memories operation surface, or the published surface drifts from the mapped endpoints.
  - Evidence: Story 18.3 published the invocable route/operation-surface contract at `docs/operations/route-surface.md` (full 45-route `/api/*` inventory, pub/sub `/dapr/subscribe` + `POST /events/ingest` operation surface, health and MCP probes, and the explicit `/process` refutation tied to code) and guards it against drift with `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` (forward code->doc route tie deriving the list from `Program.cs`, a 45-route count tie, bidirectional pub/sub + health constant ties, an MCP source-text tie, and a code-tied `/process` negative assertion). Residual OpenAPI/Swagger document emission is carried forward as `MEM-3-OPENAPI`.
- ID: MEM-3-OPENAPI
  - Status: accepted
  - Source story: 18-3-invocable-route-and-operation-surface-publication
  - Target artifact: docs/operations/route-surface.md (maintained route/operation-surface contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs; a future OpenAPI/Swagger document-generation story stays unassigned until the re-open trigger fires.
  - Re-open trigger: a downstream consumer needs a generated OpenAPI/Swagger document (machine-consumable schema for client/ACL generation) rather than the maintained route-surface contract.
  - Rationale: Story 19.2 (2026-06-30) accepts the maintained route-surface contract as sufficient for current consumers and declines to schedule OpenAPI/Swagger generation. Story 18.3 AC2 explicitly permitted "an OpenAPI document OR a maintained route-surface doc"; route-surface.md publishes the full 46-route ACL-verifiable surface and RouteSurfaceContractTests ties it to Program.cs so it cannot drift. The repo has no AddOpenApi/MapOpenApi/Swashbuckle today and no consumer needs a generated schema; standing up OpenAPI for 46 minimal-API endpoints plus the pub/sub controller is accept-until-trigger. Owner: Server / API maintainer.
- ID: MEM-4
  - Status: resolved
  - Source story: parties-consumer-integration-intake-2026-05-27
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.4)
  - Re-open trigger: concurrent same-source ingests race into duplicate/partial memory units, or consumers still require the `HXL001` suppression to ingest.
  - Evidence: Story 18.4 graduated `MemoriesClient.IngestAsync` out of `[Experimental("HXL001")]` (stable 8-param overload preserved + additive `idempotencyToken` overload; `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`), added the optional `IngestionInput.IdempotencyToken` contract property (`src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`), and closed the REST `/api/ingest` check-then-act race with an atomic Redis `SET … NX` preflight reservation (`src/Hexalith.Memories.Server/Ingestion/IngestDedupReservation.cs`, wired in `Program.cs`) keyed by idempotency-token-precedence/`sourceUri`-fallback while preserving the permanent `sourceUri → MemoryUnitId` mapping for Stories 18.5/18.6. Proven by `tests/Hexalith.Memories.Server.Tests/Ingestion/IngestDedupReservationTests.cs` (winner/loser concurrent-ingest + fail-open), `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/CheckIdempotencyActivityTests.cs` (token precedence + sourceUri fallback), `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` (token-keyed duplicate short-circuit + dual permanent record), `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientTests.cs` (stable client, token on the wire), and `tests/Hexalith.Memories.Contracts.Tests/V1/IngestionInputSerializationTests.cs` (camelCase round-trip + back-compat). Stable contract documented at `docs/dev/ingest-contract.md`; `HXL001` ledger updated at `docs/dev/experimental-apis.md`.
- ID: MEM-5
  - Status: resolved
  - Source story: parties-consumer-integration-intake-2026-05-27
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.5)
  - Re-open trigger: a consumer resolving a memory unit from a known source URI must rely on free-text search and silently degrades graph mode to local.
  - Evidence: Story 18.5 exposed an exact source-URI-keyed lookup that reads the permanent dedup record as the authoritative index (no parallel store): new route `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/by-source-uri` (`src/Hexalith.Memories.Server/Endpoints/MemoryUnitLookupEndpoint.cs`, mapped in `Program.cs`) backed by the lookup seam `src/Hexalith.Memories.Server/Ingestion/SourceUriMemoryUnitLookup.cs` (reuses `DedupKeyBuilder.BuildKey`, excludes the transient `PreflightDedupReservation` marker, and propagates Redis failures so the endpoint returns a structured `503 LOOKUP_BACKEND_UNAVAILABLE` rather than a false `404`). Surfaced through the additive `Contracts.V1` record `MemoryUnitIdLookupResponse` (registered in `MemoriesJsonContext`), the public `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` (`string?`, 404→null; D9 concrete/virtual), and the CLI diagnostic `memories search lookup` (`src/Hexalith.Memories.Cli/Commands/SearchLookupCommand.cs`, `CliExitCodes.NotFound` on miss). MCP exposure deliberately declined (operational/diagnostic resolution). Proven by `tests/Hexalith.Memories.Server.Tests/Ingestion/SourceUriMemoryUnitLookupTests.cs`, `tests/Hexalith.Memories.Server.Tests/Endpoints/MemoryUnitLookupEndpointTests.cs` (200 / structured-404 / 400 / cross-tenant / different-case / transient-reserved / Redis-down→503 / literal-route precedence), `tests/Hexalith.Memories.Contracts.Tests/V1/MemoryUnitIdLookupSerializationTests.cs` (camelCase round-trip), `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientLookupTests.cs` (path/encoding, 200→id, 404→null, error→MemoriesRemoteException), and `tests/Hexalith.Memories.Cli.Tests/Cli/SearchLookupCommandTests.cs`. Published route surface updated at `docs/operations/route-surface.md` (45→46) with `RouteSurfaceContractTests` green.
- ID: MEM-6
  - Status: resolved
  - Source story: parties-consumer-integration-intake-2026-05-27
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.6)
  - Re-open trigger: a consumer's `MemoryUnitId`-keyed mapping accumulates ghost ids after a Memories restart/contract change because the stability semantics are unspecified.
  - Evidence: Story 18.6 published the MemoryUnitId stability contract at `docs/dev/memory-unit-id-stability.md` (MemoryUnitId is an opaque id string, not derived from `sourceUri` and not guaranteed to be a ULID; same `(tenantId, caseId, sourceUri)` re-ingestion returns the same canonical id while the permanent `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}` record persists; TTL-less `expiry: null` dependency made explicit; Redis-eviction / manual-deletion / TTL / key-format-change / cross-environment-reindex loss modes documented; the dedup record is the id-resolution authority, not the backend index; Story 18.4 token records `dedup:{tenantId}:{caseId}:tok:{sha256(token)}` augment-never-replace the source-URI record; Story 18.5 `MemoriesClient.LookupMemoryUnitIdBySourceUriAsync` / `GET .../memory-units/by-source-uri` named the authoritative resolution path; Parties 'decision D1' clarified as unrelated to Memories Architecture Decision D1 'FalkorDB for MVP') with an authoritative-guarantee cross-link added at `docs/dev/ingest-contract.md` section 6. Guarded by `tests/Hexalith.Memories.Server.Tests/Ingestion/MemoryUnitIdStabilityContractTests.cs` (doc<->code ties on SaveDedupKeyActivity `expiry: null`, DedupKeyBuilder `dedup:{tenantId}:{caseId}:` / `:tok:` shapes, and SourceUriMemoryUnitLookup `DedupKeyBuilder.BuildKey`) and `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/SaveDedupKeyActivityTests.cs` (TTL-less expiry assertion); existing IngestionWorkflowTests / DedupKeyBuilderTests continue to prove stable-instance-id reuse, independent id for `dedup:` event workflows, duplicate short-circuit, and dual permanent records.
- ID: MEM-7
  - Status: resolved
  - Source story: parties-consumer-integration-intake-2026-05-27
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 18.7)
  - Re-open trigger: `MemoriesClient` is sealed or has `virtual` members removed, breaking consumer subclass-based test fixtures.
  - Evidence: Story 18.7 published the MemoriesClient mockability stability contract at `docs/dev/client-mockability.md` (reaffirms Architecture Decision D9 — concrete class, avoid the abstraction tax, extract an interface only when a second implementation arrives — and explicitly declines to add `IMemoriesClient`; documents the two supported seams: the recommended `HttpClient`/`IHttpClientFactory` boundary with a worked example and subclass override; guarantees `MemoriesClient` stays public + non-sealed with `virtual` public methods; records the breaking-change rule that sealing the class or removing `virtual` requires the D9 escape hatch (extract `IMemoriesClient`) plus a sprint change; notes the non-virtual `BaseAddress` passthrough is outside the mock seam) with companion cross-links added in `docs/dev/public-surface-stability.md` and `docs/dev/experimental-apis.md`. Guarded by `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientMockabilityContractTests.cs` (doc mandatory-claims content ties + reflection guard asserting `MemoriesClient` is public, non-sealed, exposes no `IMemoriesClient`, and that every public declared instance method is `IsVirtual && !IsFinal`, plus worked-example `[Fact]`s for both seams). The subclass seam remains proven by `tests/Hexalith.Memories.Mcp.Tests/StubMemoriesClient.cs` and the `HttpClient` seam by the `tests/Hexalith.Memories.Cli.Tests/ClientRest/*` suite; no production code changed (`MemoriesClient` already satisfied the contract).

## Deferred from: code review of story-2.7-evidence-packet-contract-mapping (2026-06-30)

- **`HasIndexedMemoryUnits` captured but never consulted** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:61] — true-empty (no index for the tenant) is not distinguished from query-empty; both yield `state: empty` + `broadenScope`, which is misleading for an unindexed tenant. Needs a recovery/state design choice (e.g. an "ingest first" recovery when `HasIndexedMemoryUnits==false`).
- **`state: empty` emitted when `returnedCount==0` but `totalCount>0` and nothing omitted** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:224] — the `returnedCount==0` short-circuit ignores `totalCount`, producing a self-contradictory empty packet. Low likelihood (requires upstream to report a contradictory result).
- **Graph summary hardcoded `Available=false`/empty even when a `graph` axisEvidence entry has a real `GraphScore`** [src/Hexalith.Memories.Contracts/V1/EvidencePacketMapper.cs:71, 132] — internal inconsistency between graph axis evidence and the graph summary section. Tolerable per spec (explicit-unavailable); graph mapping is optional/out of scope for this story.
- **RESOLVED 2026-07-05 — `EvidencePacketSource` has no freshness field** [src/Hexalith.Memories.Contracts/V1/EvidencePacket.cs] — optional `EvidencePacketSource.Freshness` and packet-level `EvidencePacketMetadata.Freshness` now cover source freshness, freshness state, produced/last-checked/expiry timestamps, and age metadata. The web cockpit and lens mappers render freshness/last-checked when present and keep unavailable boundaries when absent.

## Deferred from: story-21.2 dev (2026-07-04)

- **23.7-APPHOST-EVENTSTORE-FULLSTACK - accepted.** Current package/source compilation and the
  Memories-owned Aspire Redis/FalkorDB + Dapr ingestion lane pass, but the AppHost does not provision
  an `eventstore` gateway resource and current source/package identities do not match EventStore Story
  1.20's exact approved pins. The focused event-ingestion lane publishes directly to Memories and is not
  EventStore-to-Memories proof. This preserves the original Story 21.2 finding: case, annotation,
  memory-unit, and case-deletion mutations target the `Hexalith.EventStore.Client` gateway at Dapr app-id
  `eventstore` by default, while topology/deployment work remained outside the A3 write-boundary closure.
  The historical 2120-server-test result remains provenance, not current full-stack evidence.

  - ID: 23.7-APPHOST-EVENTSTORE-FULLSTACK
  - Status: accepted
  - Source story: story-21.2 dev; Epic 23 retrospective corrective action
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 28.1)
  - Rationale: the AppHost lacks the approved `eventstore` gateway topology and the current
    Memories-owned ingestion lane is not EventStore-to-Memories proof, so Story 28.1 retains the
    full-stack work and its explicit evidence requirements.
  - Resolution criteria: adopt the exact owner-approved EventStore source/package identities; compose one
    `eventstore` gateway resource with unambiguous `statestore`/`pubsub` ownership; run a real
    EventStore-originating publish through Dapr into Memories; prove persisted/searchable Redis and
    FalkorDB outcomes plus ignored duplicate replay; attach tenant-isolation negative evidence.
  - Re-open trigger: Story 28.1 is selected; any story or review claims EventStore-to-Memories or
    unqualified full-stack EventStore proof; or the AppHost adds an `eventstore` resource without closing
    every resolution criterion.
- **Story 24.3 graph content and honest test evidence.**

  - ID: 24.3-GRAPH-CONTENT-EVIDENCE
  - Status: carried-forward
  - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling
  - Target artifact: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md
  - Backlog home: Story 24.6
  - Owner: Murat / Test Architect and Developer
  - Rationale: The graph-content evidence is implemented and verified, but its owning Story 24.6 remains in review. Keeping the entry carried forward preserves the deferred-work lifecycle contract until the story reaches `done`.
  - Evidence: Story 24.6 implemented `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` with identical tenant A/B node IDs, topology, insertion order, graph-scoped relationship-ID collision, authenticated dual-tenant traversal, tenant-local node/edge markers, and zero foreign markers. The assertion-sensitivity control `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` plants a tenant B edge-marker literal in tenant A and proves the edge-locality assertion rejects it; it is not cross-tenant access evidence and does not mutation-test the node-marker assertions. The final authoritative real Aspire/FalkorDB owning-class run on 2026-08-13 passed 7 total, 0 failed, 0 skipped in 243.340 seconds with `MEMORIES_DAPR_PLACEMENT_HOST_ADDRESS=localhost:6050` and `MEMORIES_DAPR_SCHEDULER_HOST_ADDRESS=localhost:6060`; it supersedes every earlier method/class duration. The current verifier/runbook/authorization result is recorded once in the target story's seventh-pass verification row rather than duplicated with a competing duration here. Proof boundary: `SeedCollisionGraphAsync` writes through `falkor.SelectGraph(tenantId)` directly rather than through production ingestion, so this evidence proves authenticated read-path tenant routing and graph-content locality under collisions, not production write-path tenant selection; write-path isolation remains separately deferred. Exact commands are recorded in the target artifact and its linked implementation spec. This entry remains carried forward while Story 24.6 is in review and moves to resolved only when Story 24.6 reaches `done`.
  - Re-open trigger: Story 24.6 reaches `done` (promote this entry to `resolved`), or any change to the graph fixture, `AssertTraversalIsFixtureLocal`, `GraphQueryBuilder`, traversal route/authorization, FalkorDB tenant selection, removal/skip/rename of either the positive or assertion-sensitivity method, any claim that `GRAPH.LIST` or unit mocks prove content isolation, or a future required lane that cannot execute the real backend.

- **Story 24.3 configured vector-dimension authority.**

  - ID: 24.3-VECTOR-DIMENSION-SOURCE
  - Status: carried-forward
  - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling
  - Target artifact: _bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md
  - Backlog home: Story 24.7
  - Owner: Winston / Architect and Developer
  - Rationale: Story 24.3 compares raw and natural-language semantic dimensions only with each other, so an equally wrong pair can pass. Story 24.7 makes the requested tenant's existing `ITenantEmbeddingConfigProvider` value authoritative without recreating indexes or running migration.
  - Re-open trigger: Story 24.7 is selected, or any verifier assurance relies only on raw-versus-natural-language dimension equality.

- **Story 24.3 collision-safe semantic key-family membership.**

  - ID: 24.3-SEMANTIC-KEY-FAMILY
  - Status: carried-forward
  - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling
  - Target artifact: _bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md
  - Backlog home: Story 24.8
  - Owner: Developer and Murat / Test Architect
  - Rationale: Broad `{tenantId}:vec:*` and `{tenantId}:vecnl:*` scans include markerless raw/NL migration staging hashes and legacy nested-NL hashes, causing false marker-mismatch evidence. Because memory-unit IDs are opaque, Story 24.8 requires canonical provenance and record shape rather than prefix-only shortcuts, plus collision-shaped tests and a guarded unknown-family outcome.
  - Re-open trigger: Story 24.8 is selected, a migration or legacy tenant reports false `SemanticIsolation`, a new semantic namespace appears, or a classifier assumes reserved-looking colon text cannot occur inside an opaque memory-unit ID.

- **Story 24.3 distinct and non-destructive marker remediation.**

  - ID: 24.3-MARKER-REMEDIATION
  - Status: carried-forward
  - Source story: 24-3-physical-tenant-isolation-and-verifier-scaling
  - Target artifact: _bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md
  - Backlog home: Story 24.9
  - Owner: Winston / Architect, Murat / Test Architect, and Developer
  - Rationale: Pre-Story-24.3 active hashes can lack `tenantId`, yet current verification combines missing and foreign markers and recommends removing mismatched target-prefix hashes. Story 24.9 keeps both outcomes fail-closed but classifies missing markers as incomplete evidence, foreign values as possible contamination, and limits remediation to named-key inspection/quarantine plus tenant-scoped repair or reindex after provenance verification.
  - Re-open trigger: Story 24.9 is selected, missing markers are described as confirmed leakage, or operator guidance recommends broad prefix deletion.

## Deferred from: code review of 24-2-read-path-caching-and-tenant-list-bounding (2026-07-05)

- **24.2-RV1 — One tenant's enrichment exception fails the entire `GET /api/tenants` page.** `TenantEndpointHandlers.cs:73` — `Task.WhenAll` rethrows the first fault and discards all other already-computed summaries, so there is no per-tenant isolation. Mitigated because `BuildTenantSummaryCoreAsync` catches embedding-config exceptions and `TenantMetricsService` is designed not to throw (returns null/degraded); triggering it needs an unexpected exception such as `ObjectDisposedException` on multiplexer teardown. Re-open trigger: any change that lets `BuildTenantSummaryCoreAsync` throw for a single tenant, or a report of `GET /api/tenants` returning 500 in a multi-tenant deployment.
- **24.2-RV2 — Degraded/null metric snapshots are cached for the full summary TTL.** `TenantEndpointHandlers.cs:82` + `TenantSummaryCache.cs:49` — a summary composed during a transient backend outage (null counts / Unknown / Degraded health) is cached wholesale for the full summary TTL (default 15s), so the degraded view persists after backend recovery. AC6 letter is met (nulls preserved, not false zeros); bounded by the short default TTL. Re-open trigger: summary TTL raised toward its 120s clamp, or operators reporting stale degraded tenant health after a backend recovers.

## Deferred from: bmad-dev-auto review of spec-24-4-metric-naming-and-committed-dashboards (2026-07-05)

- source_spec: `_bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md`
  summary: Tune explicit histogram bucket boundaries for the search-duration and natural-language-description latency instruments so the committed p95 dashboard panels are accurate at seconds scale.
  evidence: Neither `memories.search.duration` nor `memories.natural.language.description.duration` configures bucket boundaries (no `AddView` in the metrics pipeline), so both use the SDK default buckets (...1000, 2500, 5000, 7500, 10000 ms). Natural-language description latency is documented at ~1-3s p95, so `histogram_quantile(0.95, ...)` over those coarse buckets can misreport a true 2.6s p95 by seconds. Pre-existing emission config surfaced by the new p95 panels; Story 24.4's "Never" boundary forbids changing emission behavior, so bucket tuning is separate work.

- source_spec: `_bmad-output/implementation-artifacts/spec-24-4-metric-naming-and-committed-dashboards.md`
  summary: Decide whether historical BMAD implementation-artifact records (stories 9.2/9.3/20.5) should be forward-referenced or updated so they stop citing pre-rename metric names.
  evidence: Story 24.4 renamed instruments (e.g. `memories_conversation_cache_hit_total` -> `memories.conversation.cache.hits`, `memories.rate_limit.rejections` -> `memories.rate.limit.rejections`), but `_bmad-output/implementation-artifacts/9-2-*.md`, `20-5-*.md`, and `7-5-*.md` still name the old instruments. These are point-in-time story records outside the spec's "source, tests, or docs" scope, so whether to rewrite history or add forward-reference notes is a judgment call the orchestrator owns.
- **24.2-RV3 — Pre-existing: two `DeleteMemoryUnitProjectionActivityTests` fail at HEAD (unrelated to Story 24.2).** `RunAsync_HappyPath_ShouldDeleteAnnotationsBeforeTargetAndSyntacticHashLast` and `RunAsync_VectorDeleteFails_ShouldKeepSyntacticHashForRetry` fail on the full server slice (2 of 2441). Verified pre-existing by stashing the 24.2 review patches and re-running the class (still 2/3 failing), so NOT introduced by read-path caching — the delete-projection Redis hash/vector ordering area, likely from a later commit (24.3/24.4/CI). Flagged during the 24.2 code review for separate triage. Re-open trigger: whoever owns the delete-projection area investigates the NSubstitute in-order sequence assertion on annotation/target/syntactic-hash deletion.

## Deferred from: bmad-dev-auto review of spec-24-5-hot-path-write-amplification-cleanup (2026-07-06)

- source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md`
  summary: Case activity legacy `failedCount` one-time backfill is pre-empted by the write-path `HashIncrement`, so a legacy case whose first post-24.5 event is `IngestionFailed` permanently undercounts its pre-existing stream failures.
  evidence: `GetFailedCountAsync` (src/Hexalith.Memories.Server/Cases/CaseActivityService.cs:141-147) backfills from the stream only when the `failedCount` summary field is absent, but `UpdateSummaryAsync` (:260-265) calls `HashIncrementAsync(failedCount)` on every `IngestionFailed`, creating the field at 1. A legacy case (no summary hash yet) whose first post-deploy event is a failure therefore reports `1` forever and never reconciles the older stream failures. A naive backfill-then-increment would double-count the just-appended event, so the fix needs an explicit "summary initialized" marker or a backfill-before-append restructuring. Related: `BackfillSummaryFromStreamAsync` (:185-232) also undercounts when a case exceeded `StreamMaxLength` and older failed entries were trimmed.
  - ID: 24.5-CASE-ACTIVITY-BACKFILL-PREEMPTED
  - Status: open
  - Source story: 24-5-hot-path-write-amplification-cleanup
  - Target artifact: `src/Hexalith.Memories.Server/Cases/CaseActivityService.cs`
  - Evidence: `GetFailedCountAsync` only backfills while the `failedCount` field is absent, but `UpdateSummaryAsync` creates that field with `HashIncrementAsync` on the first post-deploy failure; legacy stream failures can therefore remain unreconciled.
  - Re-open trigger: an operator or dashboard reports a case `failedCount` lower than the observed `IngestionFailed` events, or the summary/stream reconciliation is redesigned.

- source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md`
  summary: NL retry `EnqueueAsync` writes tenant backlog-set membership, payload hash, and sorted-set member as three non-atomic Redis ops, and the tenant set is pruned by a check-then-`SREM`; the resulting TOCTOU can strand a tenant's live retry entries and can orphan payload-hash fields on a crash.
  evidence: `EnqueueAsync` (src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs:55-57) does `SetAdd(TenantBacklogKey)`, `HashSet(payload)`, `SortedSetAdd(member)` in sequence; `RemoveTenantBacklogIfEmptyAsync` (:341-348, called from Complete :139, dead-letter :190, trim :436, corrupt-remove :338) reads `SortedSetLength==0` then `SREM`. If a completion's length-read and `SREM` interleave around a concurrent enqueue's `SetAdd`/`SortedSetAdd`, the tenant is dropped from `nl-embedding-retry-tenants` while a live member remains; `ListTenantsWithBacklogAsync` (:270-286) then skips it and the legacy KEYS fallback runs only when the whole set is empty (:260), so the entry is retried only on that tenant's next enqueue. A crash between :56 and :57 also orphans a payload-hash field with no member (DequeueBatch reads members only), leaking `GetBacklogBytes`. A correct fix needs atomic (Lua) enqueue/prune rather than op reordering.
  - ID: 24.5-NL-RETRY-TENANT-SET-ATOMICITY
  - Status: open
  - Source story: 24-5-hot-path-write-amplification-cleanup
  - Target artifact: `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs`
  - Evidence: `EnqueueAsync` writes tenant-set membership, payload hash, and sorted-set member as separate Redis operations, while `RemoveTenantBacklogIfEmptyAsync` uses a check-then-remove sequence that can interleave with enqueue.
  - Re-open trigger: a tenant's queued NL retry stops being polled while a live member remains, or payload-hash memory grows without a matching sorted-set member.

- source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md`
  summary: NL retry legacy-tenant discovery runs only when the new tenant-set is entirely empty, so pre-24.5 legacy tenant queues are never surfaced once any new enqueue populates the set.
  evidence: `ListTenantsWithBacklogAsync` (src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs:259-268) calls `ListLegacyTenantsWithBacklogAsync` only inside `if (tenantIds.Length == 0)`. During a 24.5 rollout, the first new failure for any tenant populates `nl-embedding-retry-tenants`, after which legacy tenants (queues with no tenant-set entry) are never discovered and their retries stall until each receives a fresh failure. Running the legacy KEYS scan unconditionally each poll is barred by the story's "no key scans on hot paths" boundary, so the fix needs a one-time startup migration sweep.
  - ID: 24.5-NL-RETRY-LEGACY-TENANT-DISCOVERY
  - Status: open
  - Source story: 24-5-hot-path-write-amplification-cleanup
  - Target artifact: `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs`
  - Evidence: `ListTenantsWithBacklogAsync` calls the legacy tenant discovery path only when the new tenant set is empty, so any new enqueue can hide existing legacy queues until each legacy tenant receives fresh work.
  - Re-open trigger: legacy NL retry work remains unprocessed after a 24.5 deployment that also enqueued new failures.

- source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md`
  summary: NL retry `CompleteAsync`/`IncrementAttemptsAsync` skip their optimistic condition when the current payload-hash field is null (legacy JSON member or already-deleted), so an unconditional remove can silently clobber a concurrent fresh re-enqueue for the same memory unit.
  evidence: In `CompleteAsync` (src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs:123-136) and `IncrementAttemptsAsync` (:168-176) the `Condition.HashEqual` guard is added only when `currentPayload.HasValue`; when it is null the transaction commits unconditionally and `SortedSetRemove(memoryUnitId)` + `HashDelete` run, so a fresh enqueue that wrote a payload + member for the same `MemoryUnitId` between the null `HashGet` and the transaction is deleted and the new failure is lost. Adding a `Condition.HashNotExists(payloadKey, memoryUnitId)` on the null branch would abort the transaction if a fresh payload appeared while still removing genuine legacy members; needs a regression test proving the concurrent-enqueue case.
  - ID: 24.5-NL-RETRY-NULL-PAYLOAD-CLOBBER
  - Status: open
  - Source story: 24-5-hot-path-write-amplification-cleanup
  - Target artifact: `src/Hexalith.Memories.Server/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistry.cs`
  - Evidence: `CompleteAsync` and `IncrementAttemptsAsync` add the optimistic `HashEqual` condition only when the current payload exists; the null-payload branch can remove a concurrently re-enqueued payload/member for the same memory unit.
  - Re-open trigger: a freshly enqueued NL retry disappears during legacy-format migration, or the null-payload transaction path is hardened.

- source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md`
  summary: The ingestion in-flight registry has no TTL or size cap and is pruned only by status polls or the startup gate, so un-polled fire-and-forget ingestions accumulate terminal entries unboundedly and inflate the next startup drain.
  evidence: `RedisIngestionWorkflowInFlightRegistry.TrackAsync` (src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs:33-46) only ever adds; the `IngestionWorkflow` and its activities never call `RemoveAsync`, and the only prunes are `DaprIngestionWorkflowStateReader.GetWorkflowStateAsync` (fires only when a client polls `/api/ingest/{instanceId}`) and the startup gate. A long-lived server whose ingestions are never polled grows `ingestion-workflow:in-flight` (sorted set) and `:members` (hash) without bound; the next restart's `TryCountInFlightAsync` (src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs:113-160) then issues one sequential `GetWorkflowStateAsync` (10s per-query timeout) per dead entry and can exceed the 5-minute `TotalTimeout`, so the replay gate proceeds (`event 9172`) without confirming the drain. `RemoveAsync`'s lookup-miss fallback (`FindMembersByInstanceIdAsync`, :159-174) also degrades to a full O(N) sorted-set read, compounding the drain cost. Needs terminal-state removal on workflow completion plus a TTL/size bound and batched status reads.
  - ID: 24.5-INFLIGHT-REGISTRY-UNBOUNDED
  - Status: open
  - Source story: 24-5-hot-path-write-amplification-cleanup
  - Target artifact: `src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs`
  - Evidence: `TrackAsync` adds entries but workflows never remove terminal entries directly; cleanup depends on polling or startup gating, which can leave unpolled terminal ingestions in Redis indefinitely.
  - Re-open trigger: the in-flight registry keys grow without bound, or the replay gate times out (`event 9172`) on a normal restart.

- source_spec: `_bmad-output/implementation-artifacts/spec-24-5-hot-path-write-amplification-cleanup.md`
  summary: The replay-safety in-flight registry marks itself initialized on the first `TrackAsync` against shared Redis, so a multi-replica rolling upgrade can disable the one-time enumeration fallback for a sibling replica that still has untracked pre-24.5 in-flight workflows.
  evidence: `TrackAsync` (src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs:45) unconditionally sets `InitializedKey`, and `WorkflowReplaySafetyHostedService.TryCountInFlightAsync` (src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs:122-132) runs the enumeration fallback only while the registry is empty AND uninitialized. In a rolling upgrade sharing one Redis, if the first upgraded replica proceeds past the 5-minute drain timeout (`event 9172`, already a Critical degraded state) with old pre-24.5 workflows still active and then schedules new work, `TrackAsync` sets the marker; a sibling replica starting afterward sees `IsInitialized=true`, skips enumeration, checks only tracked ids, and never observes the still-active untracked old workflows — the version-mismatch replay the gate exists to prevent. Marking initialized only after a confirmed zero-drain (not on track), or a rollout-scoped initialization signal, would close it.
  - ID: 24.5-REPLAY-GATE-ROLLOUT-MARKER
  - Status: open
  - Source story: 24-5-hot-path-write-amplification-cleanup
  - Target artifact: `src/Hexalith.Memories.Server/Ingestion/RedisIngestionWorkflowInFlightRegistry.cs`
  - Evidence: `TrackAsync` sets the shared initialized marker before the startup gate has proven a zero-drain state, so another replica can skip enumeration fallback during a rolling upgrade.
  - Re-open trigger: a multi-replica rollout replays a pre-registry in-flight ingestion workflow after another replica passed the gate.

## Deferred from: bmad-dev-auto review of spec-25-1-program-cs-decomposition (2026-07-06)

- source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md`
  summary: Ingestion endpoints dereference null JSON request bodies while setting telemetry before reaching the existing structured validation response.
  evidence: The review traced `POST /api/ingest`, `/api/ingest/url`, and `/api/ingest/directory`; each path reads `input.TenantId` or `request.TenantId` before the existing `Validate*Request` helper can return `INVALID_INPUT`. This behavior existed in the original inline `Program.cs` handlers and was preserved by the decomposition.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md`
  summary: Search source-type validation accepts numeric enum values that do not correspond to defined `SourceType` members.
  evidence: The review traced `/api/search` validation and found `Enum.TryParse<SourceType>(sourceType, ignoreCase: true, out _)` without `Enum.IsDefined`; numeric values can parse and flow into search filters. This behavior existed before endpoint extraction and is outside the mechanical decomposition scope.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md`
  summary: Graph traversal treats comma-only `edgeTypes` input as an empty explicit edge-type filter instead of defaulting or rejecting.
  evidence: The review traced `/api/tenants/{tenantId}/traverse`; `edgeTypes=","` produces an empty split result and assigns an empty `parsedEdgeTypes` list. This behavior existed before endpoint extraction and can return no edges where the default traversal would have applied.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md`
  summary: Edge-confidence promotion does not reject undefined `EdgeType` enum values after deserialization.
  evidence: The review traced `/api/tenants/{tenantId}/edges/confidence`; the payload is deserialized into `ConfidencePromotionRequest` and field presence is validated, but `Enum.IsDefined(request.EdgeType)` is not checked before the graph update. This behavior existed before endpoint extraction.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md`
  summary: Tenant provision/deletion status endpoints do not translate Dapr sidecar outages into structured `DAPR_UNAVAILABLE` responses.
  evidence: The review traced `GET /api/tenants/{tenantId}/provision-status/{instanceId}` and `GET /api/tenants/{tenantId}/deletion-status/{instanceId}`; `GetWorkflowStateAsync` exceptions flow to the generic unhandled-exception path. This behavior existed before endpoint extraction.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md`
  summary: Tenant deletion Dapr-unavailable rollback catches only `InvalidOperationException`, so other rollback failures can replace the intended 503 response.
  evidence: The review traced the Dapr-unavailable branch in `DELETE /api/tenants/{tenantId}`; rollback errors other than `InvalidOperationException` are not swallowed or logged. This behavior existed before endpoint extraction and can leave a tenant in deleting state while returning an unexpected 500.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-1-program-cs-decomposition.md`
  summary: Large extracted endpoint and service-registration files remain candidates for further focused decomposition.
  evidence: The review noted that behavior was moved into per-resource files, but `SearchEndpoints`, `CasesEndpoints`, `TenantLifecycleEndpoints`, and `MemoriesServerServiceCollectionExtensions` remain large. Story 25.1 intentionally stopped at per-resource mechanical extraction to preserve behavior; finer-grained slices should be a follow-up once the route surface is stable.

## Deferred from: bmad-dev-auto review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)

- source_spec: `_bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md`
  summary: The existing benchmark comparator happy-state progress indicator needs source-owned browser accessibility remediation before that state can be claimed axe-clean.
  evidence: The Story 17.7 Playwright axe lane initially exposed an `aria-prohibited-attr` issue on the benchmark comparator progress indicator when rendered with the happy packet fixture. Story 17.7 source ownership is specimen/test-only, so the browser specimen uses the existing empty-state fixture and keeps the happy-state remediation deferred.

- source_spec: `_bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md`
  summary: The existing benchmark comparator happy-state progress indicator remains source-owned browser accessibility follow-up work.
  evidence: The Story 17.7 review confirmed the browser specimen intentionally avoids claiming the benchmark happy-state progress-bar path as axe-clean because the underlying RCL/Fluent progress indicator produced `aria-prohibited-attr` browser accessibility evidence outside this specimen/test-only story's source ownership.

## Deferred from: bmad-dev-auto follow-up review of spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure (2026-07-06)

- source_spec: `_bmad-output/implementation-artifacts/spec-17-7-runnable-web-specimen-and-browser-at-accessibility-gap-closure.md`
  summary: Hexalith.Memories.Web.Tests — including the Epic 17 machine-checked inventory/over-claim guards — is in the .slnx test inventory but absent from every tools/test-projects.*.txt lane, so it never runs in CI.
  evidence: grep finds no reference to Web.Tests under `.github/` or `tools/`; the `test-unit-contract` CI job runs only the five projects listed in `tools/test-projects.unit-contract.txt` (Contracts/Server/Cli/Mcp/EventStore), yet that file's own header says "Keep in sync with Hexalith.Memories.slnx test inventory." The 476-test suite runs locally/pre-commit and passed here via `dotnet exec` (DiffEngine_Disabled=true), but no CI lane executes it, so the story's fail-closed inventory guards are not CI-enforced. bUnit is in-process/docker-free, so wiring Web.Tests into the unit-contract lane is viable but must be confirmed with a headless CI run that this sandbox cannot perform. Pre-existing drift broader than Story 17.7, surfaced because 17.7's headline machine-checked guards depend on CI execution.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-6-mcp-tool-executor.md`
  summary: Generic MCP tool failures direct operators to inspect server logs, but the current tool/executor path does not log the original exception.
  evidence: `McpErrorMapper.MapGeneric` emits a sanitized suggestion to inspect MCP server logs, while the pre-existing tool catch blocks and the new shared executor map the exception without an `ILogger` emission; adding redacted source-generated diagnostics requires focused logging and telemetry ownership beyond this behavior-preserving refactor.

## Deferred from: bmad-dev-auto follow-up review of spec-25-7-evidence-cockpit-ux-conformance (2026-07-11)

- source_spec: `_bmad-output/implementation-artifacts/spec-25-7-evidence-cockpit-ux-conformance.md`
  summary: The trust strip still renders the packet's confidence (evidence strength) and freshness for an unauthorized or unknown-isolation packet, even though the source-count badge is now fail-closed to "sources unavailable"; the residual confidence/freshness badges leak a coarse "strong evidence exists" inference past the authorization wall.
  evidence: `MemoriesTrustStrip.razor` gates the source-count badge on `Packet.State == Unauthorized || IsRestrictiveScope(Packet.Scope.IsolationStatus)` but gates `ConfidenceLabel`, `FreshnessText`, and `TokenBudgetText` only on `ShowPacketValues` (Packet mode), so a restrictive packet carrying real `EvidenceStrength`/`Freshness` still shows "Confidence: Strong" and the actual freshness while the count is suppressed. The exposure is pre-existing — the trust strip has always rendered confidence/freshness in Packet mode — and was surfaced incidentally because Story 25.7 hardened only the source-count badge. The intent scopes fail-closed suppression to source/axis/graph detail and explicitly forbids introducing new trust semantics, so tightening the trust-summary badges is out of scope for this story and needs a focused fail-closed trust-surface decision.

## Deferred from: bmad-dev-auto follow-up review of spec-25-8-dead-code-and-topology-cleanup (2026-07-11)

- source_spec: `/home/administrator/projects/hexalith/memories/_bmad-output/implementation-artifacts/spec-gh-29158532353-release-preflight-stale-branch.md`
  summary: Release preflight does not recognize semantic-release's alternate first-release version message when no prior tag exists.
  evidence: Repository-pinned semantic-release 25.0.5 emits `There is no previous release, the next release version is <version>`, while the pre-existing parser expects `The next release version is <version>`; this repository already has release tags, so first-release support is outside the stale-checkout incident fix.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-8-dead-code-and-topology-cleanup.md`
  summary: The new release-package topology validation — the `-PackageDirectory` throws in `tools/validate-release-packages.ps1`, the `tests/tooling/release_packages` and `tests/tooling/publish_nuget` fixtures, and the real packed-nuspec dependency closure — runs only post-merge in `release.yml`, never on the PR (`ci.yml`) lane, so package-topology and validator regressions merge green and first fail during the release run.
  evidence: `.github/workflows/ci.yml` has no step that runs `validate-release-packages.ps1 -PackageDirectory`, no `python -m unittest` over `tests/tooling/release_packages` or `tests/tooling/publish_nuget`, and no `dotnet pack`; the `release.yml` bare invocation passes no `-PackageDirectory` (skipping the whole new block), the `release_packages` fixtures run only inside `release.yml`, and the real-package throws fire only inside `semantic-release` → `pack-release.ps1`. A PR that re-adds a `Hexalith.Memories.*` ProjectReference to the Redis compatibility package, drifts the Mcp→ServiceDefaults dependency version out of lockstep, or edits the ServiceDefaults/Redis/publish tooling passes all PR checks — the only PR-lane guard, `BackendProjects_ShouldNotUseRedisCompatibilityPackageAsDependencyFacade`, asserts csproj text, not packed nuspec dependency graphs. `tests/tooling/publish_nuget/publish_nuget_test.py` is edited by this story yet is discovered by no CI lane at all. Pre-existing CI-lane architecture (release validation has always run at release time), surfaced by Story 25.8 adding substantial new post-merge-only validation; closing it means wiring the tooling fixtures (and ideally a pack plus `-PackageDirectory` validation) into the PR lane, which must be confirmed with a headless CI run this sandbox cannot perform.

## Deferred from: code review of 25-3-shared-route-table-and-client-consolidation (2026-07-11)

- source_spec: `_bmad-output/implementation-artifacts/25-3-shared-route-table-and-client-consolidation.md`
  summary: Published commit `8e92fe7` advanced five root submodule pointers even though Story 25.3 excluded submodule edits.
  evidence: Removing those historical gitlink changes now would require rewriting published history or rolling dependencies back across later `main` commits. The user finalized remediation commit `eb959d7` without authorizing either destructive operation, so the scope deviation remains documented rather than mutating current dependency state.

## Deferred from: code review of spec-25-4-contract-persistence-separation-and-route-versioning (2026-07-12)

- source_spec: `_bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md`
  summary: NonDisposingRateLimiter has no disposed-state guard; a host-shutdown ordering race can call a disposed inner limiter and throw ObjectDisposedException on an in-flight request.
  evidence: `src/Hexalith.Memories.Server/RateLimiting/NonDisposingRateLimiter.cs:24` delegates `AcquireAsyncCore`/`AttemptAcquireCore` blindly to `_inner`; `InboundRequestRateLimiter.DisposeAsync` (`src/Hexalith.Memories.Server/RateLimiting/InboundRequestRateLimiter.cs:56`) disposes every shared inner `FixedWindowRateLimiter` while the ASP.NET partitioned limiter may still hold the `NonDisposingRateLimiter` wrappers. Low-impact (shutdown only) and the fix (disposed-state guard or shutdown-ordering) is a design choice, so deferred rather than patched. The wrapper correctly solves the framework idle-eviction disposal it was written for; only the shutdown corner is unguarded.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md`
  summary: The array element values of `deletedBackends`/`compensatedBackends` changed vocabulary (RediSearch→syntactic, RedisVector→semantic, FalkorDB→graph, RedisDataKeys→state); the JSON keys are pinned but no test pins the workflow-emitted values.
  evidence: `[JsonPropertyName]` preserves the keys, but `TenantDeletionWorkflow.cs`/`TenantProvisioningWorkflow.cs` now emit axis names in the arrays. `TenantProvisioningResultSerializationTests` uses arbitrary sample values (e.g. `["RediSearch","RedisVector"]`) that no longer reflect production output, so a regression in the emitted vocabulary would not be caught. Intended part of the retrieval-axis migration; deferred as a minor test-pin gap. Any downstream ACL/runbook that string-matches the old element values would break silently — relates to the "release as breaking" decision recorded in the story's Review Findings.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md`
  summary: [Decision resolved — document & accept] Commits `94eb4c8` and `376096d` bumped the `references/Hexalith.EventStore` and `references/Hexalith.FrontComposer` submodule gitlinks despite the story's "Never edit submodules" constraint.
  evidence: Both gitlink changes are already published on `main`; `Hexalith.FrontComposer` (Web UI composer) is unrelated to the contract/persistence/route scope of Story 25.4. User decision 2026-07-12: document and accept rather than rewrite published history (same resolution as the Story 25.3 review of commit `8e92fe7`). No revert performed. If a future release surfaces an unintended FrontComposer or EventStore change, revisit this entry.

- source_spec: `_bmad-output/implementation-artifacts/spec-25-4-contract-persistence-separation-and-route-versioning.md`
  summary: [Decision resolved — release-PR action] The intentional breaking route/CLR cutover landed as `feat:` with no `BREAKING CHANGE:` footer (and test-only commit `94eb4c8` is mislabeled `feat:Add`).
  evidence: User decision 2026-07-12: leave landed commit messages as-is (no history rewrite) and add a `BREAKING CHANGE:` footer to the eventual release/squash-merge PR so the generated CHANGELOG flags the `/api/v1` route + public-CLR-rename cutover as breaking for downstream consumers. Pre-GA (0.x) semver impact is limited, but the CHANGELOG breaking flag and Design-Notes intent ("must be released as a breaking refactor") require the footer. Action owner: whoever cuts the Epic 25 release PR.

## Deferred from: code review of spec-26-1-production-deployment-artifacts (2026-07-12)

- source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md`
  summary: [HIGH — gating, environment-blocked] The mandatory no-skip disposable-cluster DAPR rollout (`tools/verify-production-deployment.ps1`) has never executed; every runtime AC (AC-3/4/5 aggregate-health/degradation/fail-closed + AC-1 container-start) rests on it.
  evidence: Dev sandbox lacks `docker`/`kind`; the story is correctly at `review`, not `done`. Must run green with ZERO skips on CI/an operator cluster before `done`. Prerequisite: the verifier's image-tag naming mismatch (patch finding, `verify-production-deployment.ps1:189-190` vs `publish-containers.ps1:64,72`) will likely make the first run fail at `docker tag` — fix it before relying on this gate. Re-open trigger: any change to the deploy topology, health/ACL semantics, or container publication.

- source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md`
  summary: [MEDIUM — hardening beyond AC] Redis Stack and FalkorDB containers run as root (only `allowPrivilegeEscalation:false` + `drop:[ALL]` + seccomp; no `runAsNonRoot`/`runAsUser`).
  evidence: `deploy/kubernetes/base/{redis,falkordb}-statefulset.yaml`. AC-1 mandates non-root only for Server/MCP (both UID 1654, satisfied). Making the data stores non-root needs `fsGroup`/PVC-permission handling for `/data`. Re-open trigger: a security-hardening pass on the deployment topology, or a Pod Security Standards enforcement decision.

- source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md`
  summary: [MEDIUM — hardening beyond AC] No NetworkPolicies or Pod Security Standards in the production deployment.
  evidence: `deploy/kubernetes/base/**` has zero `NetworkPolicy` and no `pod-security` namespace labels; app port 8080 (no Service) is reachable by pod IP cluster-wide and health endpoints are anonymous. Not required by any AC. Re-open trigger: production network-segmentation / PSS requirement.

- source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md`
  summary: [LOW — drift seam] The cross-tenant cache-safety guard validates a container-bundled copy of the Conversation component, not the deployed one.
  evidence: `NaturalLanguageDescriptionOptionsValidator` reads `deploy/dapr/components/conversation-llm.yaml` (baked into the Server image); DAPR loads `deploy/kubernetes/base/dapr/conversation-openai.yaml`. Both `responseCacheTTL: 0s` today and the Production no-TTL branch (event 9165) closes the missing-material hole, but a nonzero TTL set on the deployed component is invisible to the guard. Near-best-achievable (an app cannot read a control-plane component). Re-open trigger: any change to `responseCacheTTL` handling or the component material path.

- source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md`
  summary: [LOW — cleanup] Two divergent DAPR config sources, plus an orphaned in-namespace `eventstore` identity.
  evidence: `deploy/dapr/config.yaml` + `deploy/dapr/components/*` were rewritten but are not consumed by the authoritative `kubectl kustomize` render (which uses `deploy/kubernetes/base/dapr/*`); they must be hand-synced (one file is load-bearing only because the Server `.csproj` copies it into the image). `eventstore` gets a namespace-local ServiceAccount/Role/RoleBinding with no workload deployed here (external publisher by design). Re-open trigger: any DAPR component/config edit, to avoid the two copies drifting.

- source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md`
  summary: [LOW — hardening] Server/MCP images are tag-only with `imagePullPolicy: IfNotPresent` while data stores are digest-pinned (`@sha256:`).
  evidence: `deploy/kubernetes/base/{server,mcp}-deployment.yaml`. Safe only while semantic-release tags stay immutable; a reused tag lets nodes silently run a stale cached layer with no digest to detect it. Re-open trigger: a supply-chain/image-provenance hardening decision.

- source_spec: `_bmad-output/implementation-artifacts/spec-26-1-production-deployment-artifacts.md`
  summary: [LOW — unverified] `readOnlyRootFilesystem: true` with only `/tmp` writable may fault ASP.NET Core Data Protection.
  evidence: The default Data Protection key ring path (`~/.aspnet/DataProtection-Keys`) is not under the single writable `emptyDir` at `/tmp`; if antiforgery/cookie/ephemeral key material is ever touched, the app faults or warns. Unverified — no gate that ran boots the app under the read-only rootfs. Re-open trigger: the disposable-cluster rollout running, or adding any Data-Protection-dependent feature.

## Deferred from: code review of story-26.2 (2026-07-13)

- source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md`
  summary: [LOW] Case-scoped restore does not enforce per-record case membership.
  evidence: `RestoreDataPlaneActivity.RunAsync` (`:71-124`) restores every case/unit/edge in the envelope and never reads `input.CaseId`; `ImportRequestValidator` checks only `manifest.CaseId`. No cross-tenant impact (caller is tenant-authorized). Re-open trigger: hardening the import validator to reject records outside the route case, or a multi-case case-scoped-export defect.

- source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md`
  summary: [LOW] Unknown edge `origin` is silently coerced to `Inferred` on restore.
  evidence: `RestoreDataPlaneActivity.RestoreEdgeAsync` (`:232-235`) rewrites an unrecognized/future `origin` to `EdgeOrigin.Inferred` rather than preserving or rejecting it — a fidelity change on an audit field the story claims to round-trip exactly. Only fires on corrupt/foreign export data. Re-open trigger: an export produced by a newer edge-origin schema.

- source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md`
  summary: [LOW] No operation-level idempotency token — concurrent/duplicate import POSTs run duplicate full re-embeds.
  evidence: `ImportEndpoints.HandleImportAsync` (`:147,164`) mints a fresh GUID instance id per request and unconditionally schedules a new `RestoreWorkflow`. End state converges (HSET overwrite + graph MERGE) so AC5's idempotency clause holds; the impact is doubled embedding-provider cost/load and interleaved writes on a retry/double-submit. Re-open trigger: an operator restore-cost incident, or a decision to reject a second in-flight restore per tenant.

- source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md`
  summary: [LOW] Re-index treats a missing syntactic hash as success; `RestoredMemoryUnits` counts the data-plane total.
  evidence: `RestoreReindexUnitActivity.RunAsync` (`:85-95`) returns `RestoreReindexResult(id, 0)` when the syntactic hash is absent, and `RestoreWorkflow` (`:68-71`) reports `RestoredMemoryUnits` from the data-plane count — so a partially-failed restore could report `completed` with full counts and units that are `Indexed` but have no `:vec:` vectors. Largely unreachable in the happy path (the data-plane activity writes every hash first and fails the workflow if it can't). Re-open trigger: any change that decouples data-plane restore from re-index, or an observed partial-restore incident.

- source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md`
  summary: [LOW — hygiene, not a defect] Line-ending normalization churn folded into the feature commits.
  evidence: ~2,500 diff lines are LF→CRLF flips (correct direction, toward the repo's required CRLF standard), including `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs` (959 lines, 0 substantive changes). Mixing a mass line-ending normalization into `feat` commits inflates the diff and can mask real edits. Re-open trigger: next time a bulk normalization is needed — isolate it in a dedicated `chore` commit.

## Story 26.3 Explicit Integration Deferrals (2026-07-13)

The following scenarios replace legacy runnable placeholders with literal xUnit skips. Each entry names the current missing seam, its owner, and the exact condition that makes the scenario runnable.

- **26.3-PRIVATE-HOST-FIXTURE - accepted.** The shared Aspire ingestion fixture sets `Ingestion__UrlFetcher__AllowPrivateHosts=true` before AppHost startup and cannot vary that startup-only option per test.

  - ID: 26.3-PRIVATE-HOST-FIXTURE
  - Status: accepted
  - Source story: 26-3-integration-stub-closure
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/UrlIngestionIntegrationTests.cs
  - Re-open trigger: add an isolated `AllowPrivateHosts=false` AppHost fixture variant that proves rejection creates neither workflow nor Redis/DAPR state.
  - Rationale: The production validation path is unit/API covered, but the current shared topology is intentionally private-host-enabled for scripted loopback ingestion. Owner: integration test maintainer.

- **26.3-BULK-REINGEST-HICCUP - accepted.** The five-way bulk re-ingestion scenario needs deterministic per-unit missing, claimed, and Redis-write-failure control in one request.

  - ID: 26.3-BULK-REINGEST-HICCUP
  - Status: accepted
  - Source story: 26-3-integration-stub-closure
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs
  - Re-open trigger: provide a fixture-scoped claim/hiccup seam that can assign Scheduled, NotFound, Conflicted, and Errored outcomes without process-global mutation.
  - Rationale: Existing topology controls cannot inject one scoped Redis failure while preserving the four sibling outcomes and shared state store. Owner: ingestion reliability maintainer.

- **26.3-COUNTER-STAGE-BARRIER - accepted.** Exact simultaneous queued/extracting/embedding counts require deterministic workflow stage barriers.

  - ID: 26.3-COUNTER-STAGE-BARRIER
  - Status: accepted
  - Source story: 26-3-integration-stub-closure
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/RetryFailureIntegrationTests.cs
  - Re-open trigger: add fixture-scoped extraction and embedding barriers that hold six real workflows at requested stages while the public case-status API and actor state are sampled.
  - Rationale: Direct actor transitions would not prove concurrent workflow integration, and timing-only delays would be flaky. Owner: workflow test maintainer.

- **26.3-DIRECTORY-CROSS-TENANT-PERF - accepted.** The cross-tenant directory latency claim needs a recorded baseline and bounded load harness.

  - ID: 26.3-DIRECTORY-CROSS-TENANT-PERF
  - Status: accepted
  - Source story: 26-3-integration-stub-closure
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/DirectoryIngestionIntegrationTests.cs
  - Re-open trigger: an `IntegrationSlow` performance harness can create a bounded 100-file batch, record the single-tenant baseline, and assert persisted outcomes plus the two-times latency bound without CI noise.
  - Rationale: The ordinary integration lane has no stable performance baseline or load-isolated runner. Owner: performance test maintainer.

- **26.3-BATCH-STARVATION-PERF - accepted.** The 500-file batch-versus-single-ingest starvation claim requires the same missing load harness and latency baseline.

  - ID: 26.3-BATCH-STARVATION-PERF
  - Status: accepted
  - Source story: 26-3-integration-stub-closure
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/RateLimitingIntegrationTests.cs
  - Re-open trigger: an isolated performance lane can run the bounded 500-file workload, capture a control baseline, and retain per-unit Redis/actor evidence.
  - Rationale: A timing assertion in `integration-fast` would be environment-sensitive and would not prove persisted outcomes. Owner: performance test maintainer.

- **26.3-SEMANTIC-CAPABILITY-FAULT - accepted.** Semantic search shares the single `memories-vectors` Redis Stack resource with syntactic search, DAPR state, actors, workflows, and pub/sub.

  - ID: 26.3-SEMANTIC-CAPABILITY-FAULT
  - Status: accepted
  - Source story: 26-3-integration-stub-closure
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs
  - Re-open trigger: add a Development-only, request-scoped semantic capability fault that leaves RediSearch and DAPR state available and matches production exception behavior.
  - Rationale: Stopping `memories-vectors` cannot truthfully represent a semantic-only outage. Owner: search reliability maintainer.

- **26.3-ALL-BACKENDS-STATESTORE - accepted.** Stopping Redis Stack and FalkorDB also removes workflow, actor, pub/sub, and state-store availability.

  - ID: 26.3-ALL-BACKENDS-STATESTORE
  - Status: accepted
  - Source story: 26-3-integration-stub-closure
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs
  - Re-open trigger: define and implement the supported API contract for total Redis-backed control-plane collapse, then add bounded recovery assertions against that real dependency graph.
  - Rationale: The legacy `ALL_BACKENDS_UNAVAILABLE` comment assumes independent retrieval containers that the AppHost does not have. Owner: platform reliability maintainer.

- **26.3-SINGLE-AXIS-REDIS-COLLAPSE - accepted.** A Redis resource stop is not a syntactic-only outage and can prevent the service from reading authorization, tenant, workflow, and actor state.

  - ID: 26.3-SINGLE-AXIS-REDIS-COLLAPSE
  - Status: accepted
  - Source story: 26-3-integration-stub-closure
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs
  - Re-open trigger: add a request-scoped RediSearch capability fault or publish a truthful state-store-collapse contract for the single-axis endpoint.
  - Rationale: A resource stop would overclaim `BACKEND_UNAVAILABLE` syntactic-only semantics. Owner: search reliability maintainer.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-container-publication-and-rollout-verification.md`
  summary: Distinguish a confirmed missing remote manifest from registry authentication, availability, and malformed-response failures before authorizing a container push.
  evidence: The pre-existing publisher treats every nonzero `docker manifest inspect` exit as tag absence and pushes immediately, so an outage or authorization error can enter the blind retry path.

## Deferred from: code review of 26-2-backup-and-restore (2026-07-15)

- source_spec: `_bmad-output/implementation-artifacts/26-2-backup-and-restore.md`
  summary: [MEDIUM] Add representative boundary coverage for the documented 512 MiB / approximately 100K-unit restore contract.
  evidence: The current fidelity integration restores three small units and `RedisChunkReadStreamTests` reads five mocked bytes; neither observes the endpoint ceiling, real multi-chunk staging, retention renewal, or high-cardinality workflow paging. Re-open trigger: restore-size/staging hardening or the first supported large-tenant recovery rehearsal.
- source_spec: `_bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md`
  summary: Distinguish "manifest unknown" from transient errors in remote registry inspect so a transient failure cannot bypass the digest-conflict fail-closed guard before push.
  evidence: publish-containers.ps1 treats any nonzero skopeo/docker remote inspect as "tag absent" and proceeds to push; behavior predates this story and was preserved as explicit spec parity, flagged by two independent review layers.
- source_spec: `_bmad-output/implementation-artifacts/spec-fix-release-container-push-unauthorized.md`
  summary: Probe skopeo availability in release-preflight so a missing runner binary fails before NuGet publish and tag creation instead of causing a partial release.
  evidence: Container publish now hard-depends on the runner-preinstalled skopeo; the publish-time tooling-missing check fires only after NuGet packages and the release tag exist. The story spec froze "no touching preflight", so this hardening was out of scope.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-26-benchmark-quality-gate.md`
  summary: Make independent-process benchmark reproducibility a permanent fail-closed CI comparison.
  evidence: Story 26.8 retained and compared two independent benchmark processes, but the nightly lane launches one process and its in-process reproducibility check cannot detect process-initialized drift. A future lane should normalize and compare two independently generated result payloads without weakening the existing gate.

- source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md`
  summary: Reconcile the access-telemetry retention deferred-entry schema, accepted-debt validation, and proposal wording before using that evidence to close A41.
  evidence: Blind review found non-canonical `Target artifacts:` and `Re-open/claim trigger:` labels, a validator that accepts incomplete debt metadata, and contradictory proposed/applied and open-action wording in the concurrent A41 artifacts; these issues predate and are outside the one-shot tracking correction.

- source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md`
  summary: Reconcile the architecture's structured access-log storage claim with the documented JSON-console telemetry implementation.
  evidence: Blind review found that `architecture.md` describes a structured log file while `docs/dev/telemetry.md` documents console emission plus an operator-selected external pipeline; this unrelated documentation conflict predates the one-shot tracking correction.

- source_spec: `_bmad-output/implementation-artifacts/spec-one-shot-artifact-tracking.md`
  summary: Resolve opaque-ID error-code and mixed GUID-form fallback contradictions before implementing the consistency-inspect proposal.
  evidence: Blind review found that the concurrent proposal both preserves and changes the unknown-ID error contract and omits the mixed GUID-N/GUID-D backend case that can suppress fallback; this is outside the one-shot tracking correction.

- source_spec: `_bmad-output/implementation-artifacts/spec-contract-doc-drift-guard-hardening.md`
  summary: Generalize positive route discovery beyond Program.cs and top-level `Endpoints/*Endpoints.cs` files so nested endpoint files, controllers, and differently named registration files cannot evade the route-surface guard.
  evidence: Review confirmed the approved hardening preserves the pre-existing route-source scope; a future endpoint outside that scope would not enter the source-derived route count or exact-row tie.

- source_spec: `_bmad-output/implementation-artifacts/spec-contract-doc-drift-guard-hardening.md`
  summary: Generalize the `/process` negative route scan across every production host and controller source so an unexpected route cannot evade the refutation guard by appearing outside the currently enumerated files.
  evidence: Review confirmed the existing negative check reads Server Program/decomposed endpoint sources plus EventIngestionController only; broader production-source discovery predates and exceeds this contract-document hardening change.

- source_spec: `_bmad-output/implementation-artifacts/spec-consistency-inspect-opaque-id-contract.md`
  summary: Align the repository's OpenTelemetry core packages with the versions imported by the current Hexalith.Builds pointer so the exact working tree restores and builds again.
  evidence: Exact-tree restore/build fails with NU1605 because Hexalith.Builds@8e0e2da imports OTLP exporter and hosting 1.17.0 while Directory.Packages.props pins OpenTelemetry core 1.16.0; the opaque-ID change neither caused nor is authorized to alter that concurrent dependency state.

## Deferred from: code review of 27-1-access-telemetry-retention-ownership-decision (2026-07-17)

- source_spec: `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md`
  summary: Reconcile raw privacy-sensitive state on the preserved JSON-console and optional OTLP routes with the bounded lifecycle target.
  evidence: Search and source-URI events can already expose raw query, subject, or source URI values through the existing logging routes. Story 27.1 documents that pre-existing deviation and sanitizes only the accepted Dapr lifecycle path; a later scope decision must choose sanitization before provider fan-out or explicit category exclusion from durable external routes.

- source_spec: `_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md`
  summary: Restate or intentionally retire the `docs/operations/rate-limiting.md` documentation obligation dropped from the `20.5-A41-ACCESS-TELEMETRY-RETENTION` target-artifact list.
  evidence: The concurrent A41-entry rewrite in commit `8bb0708a` (sprint-change-proposal scope, not Story 27.1) replaced the old `Target artifact: docs/operations/rate-limiting.md and the future access-telemetry storage/purge implementation` line with a new target list that omits rate-limiting.md entirely, leaving that file's documentation obligation without a stated disposition. The fourth code-review pass of Story 27.1 (2026-07-17) surfaced the orphaned obligation; ownership belongs to the A41/Story 27.4 close-out coordination after Story 27.3 qualification, not this decision story.

## Deferred from: code review of spec-infrastructure-dependency-abstraction (2026-07-17)

- source_spec: `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md`
  summary: Creation-lock release in `DaprAggregateCaseMappingStore.ReleaseCreationLockAsync` deletes unconditionally and can release a rival instance's active lock after the holder's TTL lease expired.
  evidence: `src/Hexalith.Memories.EventStore/DaprAggregateCaseMappingStore.cs:92-99` calls `DeleteStateAsync` without an owner token or ETag condition. Deferred as pre-existing: the prior Redis implementation used the same unconditional `DEL` after `SET NX`+TTL, so the migration preserved (did not introduce) this semantics; revisit if the F6 store design is reworked under the D1 review decision.

- source_spec: `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md`
  summary: Remove unreferenced `RedisPlaceholder` port-constant compat surface on the next owned breaking major (F9).
  evidence: See structured entry `IDA-F9-REDISPLACEHOLDER-REMOVAL` (appended 2026-08-09).

## Deferred from: code review of 27-3-retention-verification-operations-runbook-and-a41-close-out (2026-07-18)

- source_spec: `_bmad-output/implementation-artifacts/27-3-retention-verification-operations-runbook-and-a41-close-out.md`
  summary: Reconcile the canonical project context's stale Aspire AppHost SDK version with the repository pin.
  evidence: `_bmad-output/project-context.md` still names `Aspire.AppHost.Sdk/13.3.3`, while `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj` and the reviewed story use the actual `13.4.6` SDK pin. The context drift predates Story 27.3; current source remains authoritative until its owning documentation lane repairs the canonical context.

## Deferred from: code review of spec-run-tests-and-fix-failures (2026-07-18)

- source_spec: `_bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md`
  summary: Sibling helper `ContractDocumentGuard.cs`'s private `NormalizeLineEndings` has the same repeated-CR mishandling bug just fixed in `MarkdownContractDocument.cs`.
  evidence: `tests/Hexalith.Memories.TestHelpers/Documentation/ContractDocumentGuard.cs:250-251` still uses the old `markdown.Replace("\r\n", "\n", ...).Replace('\r', '\n')` pattern (used by `FindLeakedToolCallMarkup`), which turns a doubled `\r` (e.g. `"\r\r\n"`) into an extra blank line instead of collapsing to one `\n` — the exact defect just fixed in the sibling file. Out of this spec's Code Map scope (which named only `MarkdownContractDocument.cs`); worth the same fix if the `\r\r\n` corruption risk is judged real for this file's consumers too.

- source_spec: `_bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md`
  summary: Reconcile the `20.5-A41-ACCESS-TELEMETRY-RETENTION` entry's now-partially-resolved non-canonical labels with the earlier tracking entry that named them.
  evidence: An existing entry (source_spec `spec-one-shot-artifact-tracking.md`, "Reconcile the access-telemetry retention deferred-entry schema...") named both `Target artifacts:` and `Re-open/claim trigger:` as non-canonical labels needing reconciliation. This spec's fix renamed both labels on the `20.5-A41-ACCESS-TELEMETRY-RETENTION` entry to the canonical singular form, but the other issues that same tracking entry names (a validator accepting incomplete accepted-debt metadata, contradictory proposed/applied and open-action wording) remain unresolved. Flagging so the earlier entry isn't treated as fully obsolete, nor its label-naming half duplicated as new work.

- source_spec: `_bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md`
  summary: The `[Collection(...)]` convention preventing cross-test pollution of `AccessTelemetryLifecycleMetrics`'s static counter is enforced only by an XML doc comment, not by tooling.
  evidence: `AccessTelemetryLifecycleMetricsTestCollection`'s doc comment states every test class touching the static `Records` counter via `MeterListener` "MUST be annotated" with the collection attribute, but nothing (analyzer or reflection-based guard test) verifies this. A third class added later that records to or listens on the counter without the attribute would silently reintroduce the exact flake this spec fixed, surfacing only as an intermittent CI failure. The same gap pre-exists for `Hexalith.Memories.Server.Tests`'s `TelemetryTestCollection`, suggesting a repo-wide guard test (e.g. reflection over `MeterListener` usages cross-checked against `[Collection]` attributes) would be the durable fix.

- source_spec: `_bmad-output/implementation-artifacts/spec-run-tests-and-fix-failures.md`
  summary: `MinimumDotnetSdkVersion` and its "10.0.302" user-facing message strings are duplicated as separate literals across multiple call sites instead of derived from one source of truth.
  evidence: `src/Hexalith.Memories.Cli/Quickstart/PrerequisiteChecks.cs:27`'s `MinimumDotnetSdkVersion` constant and `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs:148`'s `"Install .NET SDK 10.0.302 or newer and retry."` string (plus other CLI message sites) each hardcode the version independently. This is the exact duplication pattern that caused the drift bug this spec fixed (the constant fell behind when messaging was bumped); deriving all user-facing strings from `MinimumDotnetSdkVersion.ToString()` would prevent recurrence, but is a refactor beyond this bugfix spec's scope.
## Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-21)

- Concurrency safety of the Dapr access-telemetry state store under actor-failover split-brain.
  - ID: 27.3-CR1
  - Status: carried-forward
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3)
  - Target artifact: src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs
  - Re-open trigger: the pending C1 two-writer / partial-commit deployment probe runs against PG-ONPREM-1.
  - Rationale: WriteRecordAndIndexAsync/DeleteAndVerifyAsync have no retry loop and empty-etag FirstWrite on first bucket/catalog creation is last-write-wins, so a rare split-brain window could drop an expiry-index entry. Not reachable under the single global turn-based AccessTelemetryLifecycleActor in normal operation, and fail-closed (ETag conflict throws -> at-least-once retry; orphaned record bounded by its own TTL). The ADR assigns two-writer collision / partial-commit proof to the C1 deployment probe, not unit tests.

- Live test-count totals recorded in the 27.3 Change Log predate current HEAD.
  - ID: 27.3-CR2
  - Status: resolved 2026-07-26 — the live runner recount executes in this sandbox and was run again by dev-story.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
  - Re-open trigger: before Story 27.3 advances to done, or when any File List assembly changes.
  - Evidence: recount executed 2026-07-26 by dev-story on a clean tree at HEAD `a6753c11` with fresh Release builds (0 warnings, 0 errors). Pre-development: Server.Tests 2,190 / `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`, IntegrationTests 297 / `7836151bdf59ff8712f59911ed138a2f7afc792a7c4d2415c64122695c163856`, AccessTelemetry.Tests 55 / `973244b8ebcdfd55eeaf01ba56b8f33a1836aee158a98906817b5a5b2e3e60ef`, Cli.Tests 384 / `55e179bb6678fb671b1b342eeef71876b5f2f2c6106903c36507bb16769de312`. Command: `DiffEngine_Disabled=true dotnet exec <assembly> -list methods -noLogo | grep -E '^Hexalith\.'`.
  - Rationale: The entry's recorded figures (Server 2,188 / Integration 297 / AccessTelemetry 43) were stale: they predated the chunk-1 and chunk-2 review patches. The correct pre-development figures at HEAD `a6753c11` are recorded in the Evidence field above, and the post-development figures are in the 2026-07-26 `dev-story` Change Log row. The original claim that a full live runner recount "could not be executed in this sandbox" is false and is corrected here.

- recordId charset is not validated before it is interpolated into the Dapr state key.
  - ID: 27.3-CR3
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3)
  - Target artifact: src/Hexalith.Memories.AccessTelemetry/Lifecycle/DaprAccessTelemetryStateStore.cs
  - Re-open trigger: a RecordId containing '/' or other key-delimiter characters can reach GetRecordKey/GetBucketKey.
  - Rationale: GetRecordKey builds `records/{shard}/{recordId}` and GetShard only guards null/whitespace; confirm the AccessTelemetryRecord contract constrains RecordId to a safe charset, otherwise add explicit validation.
- Recompute the 27.3 Change Log Server.Tests story-vs-external split at the final-chunk reconciliation.
  - ID: 27.3-CR4
  - Status: resolved 2026-07-26 — the Server story/external attribution is restated and the recompute is done.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
  - Re-open trigger: before Story 27.3 advances to done / at the final-chunk code-review ledger reconciliation.
  - Evidence: the authoritative equation is `2,157 create + 6 Story 27.3 + 1 Story 31.1 + 26 external = 2,190`, recorded by the 2026-07-26 chunk-3b `code-review` Change Log row and restated in the 2026-07-26 `dev-story` row. It supersedes both the recorded `+1/+30` and this entry's own `+5/+26` target, which was correct only at Server 2,188. The `+1` is `OpenBaoDeploymentProfile_IsPinnedTlsOnlyPersistentAndInternal`, which follows Story 31.1 with `deploy/openbao/values.yaml` per the 2026-07-26 Administrator decision. Live discovery at HEAD `a6753c11` confirms Server 2,190 at hash `bd27c3da547f6efacc2fc9ce9abd2360794c77e52e4a5fd7c6a4a5e73a28b4d0`; the 2026-07-26 dev-story phase added no Server method, so the equation is unchanged after it.
  - Rationale: 4 Story-27.3 C1 methods (Adr_C1SourceEventMapping, Adr_C1TypedStateAndNullableMapping, Adr_C1QueryAndErrorMappings, Adr_ProductionAdapterQualification in AccessTelemetryRetentionDecisionTests.cs 6->10; plus ProductionDeploymentArtifactsTests +2) are booked under the +30 external delta rather than the +1 story delta. Recompute with live discovery (expected Server +5 story / +26 external) when the final review chunk finalizes the ledger. Administrator approved deferring the recompute to the final chunk on 2026-07-21.

- Split the four-image release/publish pipeline out of Story 27.3 into a newly numbered story.
  - ID: 27.3-CR5
  - Status: resolved 2026-07-26 — split executed by the approved Sprint Change Proposal 2026-07-26 into Epic 30 / Story 30.1 (`epics.md`), registered in `sprint-status.yaml`, and removed from Story 27.3's File List.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 1/3)
  - Target artifact: _bmad-output/planning-artifacts/epics.md
  - Re-open trigger: before Story 27.3 advances to done; the release/publish-pipeline work must own a separate story.
  - Evidence: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-26-readiness-coherence-and-27-3-splits.md` (approved 2026-07-26) enumerates the ten transferred paths; `_bmad-output/planning-artifacts/epics.md` carries Epic 30 / Story 30.1; `_bmad-output/implementation-artifacts/sprint-status.yaml` registers `30-1-...` as `backlog`; Story 27.3's File List no longer declares any of the ten paths.
  - Rationale: The four-image publish/partial-recovery pipeline (CiTestInventoryTests.cs + tests/tooling/publish_containers/*) is independently demonstrable and was ledgered as an external CI/CD lane, yet is bundled into the single C1 adapter-qualification slice. Administrator approved splitting it into a new story via correct-course on 2026-07-21; 27.3's File List and ledger shrink to adapter/qualification scope.

## Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 2 (2026-07-21)

Chunk 2 = deployment manifests + docs (18 File-List paths). Intermediate chunked review; does not finalize the ledger or advance status. Chunk 3 (tooling + CI) remains.

- Fail-closed `done` blockers: chunk 3 unreviewed; live method/case recount not runnable in this sandbox; Server story/external split +1/+30 -> +5/+26 (see DW 27.3-CR4). Story stays `in-progress`.
  - ID: 27.3-CR18
  - Status: resolved 2026-07-26 — chunk 3a and chunk 3b are now reviewed and the live recount ran successfully; the surviving obligations are DW 27.3-CR4 (Server attribution) and the open review action items. (`superseded` is not one of the register's four documented statuses; corrected to `resolved` on 2026-07-26 by dev-story.)
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
  - Re-open trigger: superseded; do not reopen under this ID.
  - Evidence: chunk 3a (2026-07-26, HEAD `159d7216`) and chunk 3b (2026-07-26, HEAD `c9dfb06f`) are both recorded in **Code Review Evidence** of the story file, and the live recount executed successfully at `159d7216` (Server 2,190 / IntegrationTests 297 / AccessTelemetry.Tests 55). The surviving obligation is tracked separately as DW 27.3-CR4.
- Clock NetworkPolicy egress to TCP/443 is unrestricted (no `to:`); tighten to real UTC-source CIDRs before enablement. [deploy/kubernetes/base/access-telemetry-network-policy.yaml:99]
  - ID: 27.3-CR19
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3)
  - Target artifact: deploy/kubernetes/base/access-telemetry-network-policy.yaml
  - Re-open trigger: before Production lifecycle enablement; the clock egress must be restricted to the real UTC-source CIDRs once the three `.example.invalid` authorities are replaced.
  - Rationale: The clock NetworkPolicy allows egress on TCP/443 with no `to:` selector, so the trusted-time workload can reach any address on the internet. The real UTC-source CIDRs are not knowable while all three configured authorities are `.example.invalid` placeholders, so narrowing the rule now would encode a fiction. Owner: clock-authority owner. Consequence: an unrestricted egress path exists on a workload that is scaled to zero and fail-closed.
- ~~`maxConns: 64` x 2 replicas (128) can exceed PostgreSQL `max_connections=100` under the C1 two-writer load; reconcile before/at the load probe.~~ [deploy/kubernetes/base/dapr/access-telemetry-store.yaml:25] — **resolved 2026-07-26 (dev-story)**: `maxConns` lowered to `40`, so `2 x 40 + 3 superuser-reserved + 10 evidence sessions = 93 <= max_connections 100`. The derivation is a comment on the metadata entry and is enforced by the new `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections`, which failed RED at `141 > 100` before the fix.
- ~~Verification coverage gap: `skipVerify:"false"`, pg_hba `hostnossl...reject`, init-SQL least-privilege grants, new RBAC secret-reader Roles, `actorStateStore:"true"`, and the telemetry ACL are unbound by static guard tests; add assertions to the chunk-1 guard tests.~~ [tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs] — **resolved 2026-07-26 (dev-story)**: all six surfaces are now bound by `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryProfileSecurityContractsAreBound`. Because the guard passed on first authoring, it was mutation-proven rather than RED-proven: six independent drift injections (`skipVerify:"true"`, `actorStateStore:"false"`, dropped `hostnossl ... reject`, RBAC verbs widened to `get,list`, ACL `defaultAction: allow`, and an extra secret smuggled into `allowedSecrets`) each failed the suite, and the baseline returned green after every revert.
- Pre-enablement operational hardening: probe `wget`/`sh` dependency, missing metrics ingress, startup-vs-initTimeout cold-start race, terminationGracePeriod/PDB node-drain block, manual restart on password/CA rotation. [deploy/kubernetes/base/access-telemetry-deployments.yaml]
  - ID: 27.3-CR20
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3)
  - Target artifact: deploy/kubernetes/base/access-telemetry-deployments.yaml
  - Re-open trigger: before Production lifecycle enablement; each named hardening item must be resolved or accepted with an owner.
  - Rationale: Five independent pre-enablement items on a workload that is scaled to zero: the exec probes require `wget` and `/bin/sh` in images that are still `:0.0.0` placeholders; there is no metrics/monitoring ingress, so Prometheus reports NoData; the 60s startup probe races the store's `initTimeout: 1m` on a cold start; `terminationGracePeriodSeconds: 120` plus a `minAvailable: 1` PDB on a single replica blocks node drain; and DB/OpenBao password and TLS-CA rotation need a manual sidecar restart because HotReload is off for an actor state store. Owner: Hexalith Platform Operations. Consequence: none today (replicas are zero); each becomes live at enablement.
- Docs: release-runbook four-image expansion belongs with DW 27.3-CR5; ADR byte-bucket boundary overlap and `edgeTypeCount>16` ambiguity; verify ADR `Story 27.2 C1 mapping` block attribution. [docs/dev/release-runbook.md; docs/dev/adr-27.1-001-access-telemetry-lifecycle.md]
  - ID: 27.3-CR21
  - Status: open (release-runbook arm transferred to Story 30.1 on 2026-07-26 and reassigned to Story 30.3 on 2026-07-27 by the approved Sprint Change Proposal 2026-07-27, which owns the four-image expansion of `docs/dev/release-runbook.md`; Story 30.5 owns its cutover and rollback sections. The ADR arms remain Story 27.3's)
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3)
  - Target artifact: docs/dev/adr-27.1-001-access-telemetry-lifecycle.md
  - Re-open trigger: before Story 27.3 advances to done; the ADR byte-bucket boundary overlap and the `edgeTypeCount>16` clamp-vs-reject ambiguity must be resolved.
  - Rationale: Two documentation defects in the ADR that no code reads today: adjacent byte-bucket labels overlap at their 64KiB/1MiB/10MiB boundaries, so a value exactly on a boundary has two valid labels, and the behaviour for `edgeTypeCount > 16` is unspecified between clamping and rejecting. The `Story 27.2 C1 mapping` block's attribution also needs confirmation. Owner: ADR owner. Consequence: an implementer reading the ADR can pick either reading; no shipped code depends on the ambiguity yet. The release-runbook arm of this entry transferred to Story 30.1 on 2026-07-26.

### DW 27.3-CR7 - create-story scope verifier is inert after the 27.3 split rename

  - ID: 27.3-CR7
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (dev-story, 2026-07-26)
  - Target artifact: _bmad-output/implementation-artifacts/tests/27-3-create-story-scope-evidence.md
  - Re-open trigger: before Story 27.3 advances to done; the embedded scope verifier must exit 0.
  - Rationale: The embedded verifier's executable constants at lines 69-76 (`KEY`, `STORY`, `MATRIX`) still name the pre-split `27-3-retention-verification-operations-runbook-and-a41-close-out` key/path and the `27-3-retention-verification-evidence.md` matrix. Commit `f474db15` renamed the story to `27-3-production-adapter-and-deployment-profile.md` and the matrix to `27-3-adapter-profile-evidence.md` under the approved 2026-07-20 course correction, so the verifier now exits 1 with `missing governed artifact: …` and has been inert since the split. The status-parity, monotonic-transition, unique-YAML-key, and baseline-relative File List assertions added by earlier review findings are therefore not running. The recorded creation diff at lines 16-34 is append-only history and must not be rewritten; only the three constants are stale. Found by dev-story on 2026-07-26 while revalidating the story's own gates; not repaired there because this is a create-story-phase governance artifact and the story phase ledger does not authorize dev-story to rewrite another phase's evidence verifier.

## Deferred from: other sources, re-filed 2026-07-26 by code review (chunk 3b)

The five structured entries below (`21.10-A4-VERIFY` and the four `REL-*` entries) were filed under the Story 27.3 chunk-2 code-review heading, which does not own them: `21.10-A4-VERIFY` is an Epic 21 retrospective action and the four `REL-*` entries were surfaced on 2026-07-25 by `spec-gh-30146368778-fix-tenants-release-startup-failure.md`. They are re-filed here without altering their content, IDs, status or triggers.

### DW 27.3-CR6 - OpenBao secrets platform is an independent slice (split approved)

  - ID: 27.3-CR6
  - Status: resolved 2026-07-26 — split executed by the approved Sprint Change Proposal 2026-07-26 into Epic 31 / Story 31.1 (`epics.md`), registered in `sprint-status.yaml`, and removed from Story 27.3's File List. The static file-based seal and namespace-wide 8200 ingress are carried into Story 31.1's security-approval acceptance criterion.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 2/3)
  - Target artifact: _bmad-output/planning-artifacts/epics.md
  - Re-open trigger: before Story 27.3 advances to done; the OpenBao platform + runtime secretstore migration must own a separate story.
  - Evidence: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-26-readiness-coherence-and-27-3-splits.md` (approved 2026-07-26) enumerates the six transferred paths; `_bmad-output/planning-artifacts/epics.md` carries Epic 31 / Story 31.1; `_bmad-output/implementation-artifacts/sprint-status.yaml` registers `31-1-...` as `backlog`; Story 27.3's File List no longer declares any of the six paths.
  - Rationale: The OpenBao `hexalith-keys` secrets platform (deploy/openbao/values.yaml, namespace.yaml, service-account-hardening.yaml, smoke-test.yaml, docs/operations/openbao.md) and the runtime `secretstore` migration from Kubernetes to hashicorp.vault (scopes eventstore/memories) are an independently-deployable operations platform bundled into the single PG-ONPREM-1 C1 qualification slice, which the Slice Proof and spec (a general operations platform 'returns to planning') do not authorize. Administrator approved splitting it into a new story via correct-course on 2026-07-21; 27.3's File List and ledger shrink to the PG-ONPREM-1 adapter and its secret backing. The static file-based OpenBao seal (key in a Kubernetes Secret beside the data) and namespace-wide 8200 ingress must be surfaced to the security approver as accepted single-node limitations of that split story.

  - ID: 21.10-A4-VERIFY
  - Status: open
  - Source story: Epic 21 retro action #4 (spec-redisstack-migration-integration-lane.md); surfaced by adversarial/edge/verification-gap review 2026-07-21
  - Target artifact: tools/verify-integration-fast-coverage.py
  - Re-open trigger: any required integration-fast surface is later marked Skip= or conditionally skipped; or the standing in-lane failure (OpenBaoTopologyIntegrationTests) is fixed and a green-only enforcement backstop is wanted; or a maintainer needs the gate to prove execution rather than presence.
  - Rationale: verify-integration-fast-coverage.py asserts each required surface class is PRESENT in the TRX TestDefinitions (executed_classes harvests className from every <UnitTest> regardless of <UnitTestResult outcome>), guarded only by a lane-aggregate executed>0 check - so a required class whose tests are all Skip=/NotExecuted still satisfies the gate as long as other lane tests ran. Additionally the "Verify fast integration coverage evidence" step in ci.yml has no if: always(), so it is skipped whenever the fast lane is red, and nightly.yml does not run the verifier at all - leaving enforcement inert on red lanes. Hardening: intersect required classNames with results whose outcome is Passed/Failed (executed, not NotExecuted); add a fixture TRX test (skipped-only required class -> exit 1) mirroring tests/tooling/coverage_gate; add a red-lane/nightly enforcement backstop. Pre-existing; affects all required surfaces in integration-fast-required-surfaces.txt, surfaced while enforcing the migration surface for Epic 21 retro action #4.

  - ID: REL-EVENTSTORE-EXPECTED-COUNT
  - Status: open
  - Source story: spec-gh-30146368778-fix-tenants-release-startup-failure.md; surfaced by all three review layers 2026-07-25
  - Target artifact: references/Hexalith.EventStore/scripts/validate-publication-preflight.sh, .github/workflows/release.yml, tests/Hexalith.EventStore.Contracts.Tests/Packaging/ContainerPublishingGovernanceTests.cs
  - Re-open trigger: before Hexalith.EventStore bumps its domain-release.yml pin off cf04c419378dfe1bd3c41a9244b5e3283092056e (i.e. before editing ApprovedBuildsReleaseSha).
  - Rationale: --expected-package-count is now required on the shared publication preflight, and domain-release.yml requires the matching expected-package-count input when publish-containers is true. EventStore's wrapper passes neither and its release.yml declares neither. It is insulated only by its pin to an ancestor commit. The moment that pin is bumped, its first release fails at verifyReleaseCmd with argparse exit 2 - and both repos' suites report green, because ContainerPublishingGovernanceTests enumerates the wrapper's flags but was not extended to require the new one. EventStore must add --expected-package-count 14 to the wrapper, expected-package-count: 14 to release.yml, and matching assertions to that governance test. Deliberately out of scope here: this spec was scoped to Tenants plus the shared Builds generalization, and touching EventStore was an explicit Ask First boundary.

  - ID: REL-CONTAINER-MULTI-MAPPING
  - Status: open
  - Source story: spec-gh-30146368778-fix-tenants-release-startup-failure.md; surfaced by adversarial review 2026-07-25
  - Target artifact: references/Hexalith.Builds/Github/publish-containers/publish-containers.sh, publication_preflight.py
  - Re-open trigger: when any caller declares more than one container-projects mapping.
  - Rationale: publish-containers.sh calls the preflight once per container mapping, all writing into the same evidence directory, and publication_preflight.py fails with preflight-phase-collision when publication-preflight.container.json already exists. Tenants and EventStore each declare exactly one mapping, so the multi-mapping contract that domain-release.md advertises would fail on the second project - after the first image has already been pushed. Pre-existing; not caused by the caller-declared package-count change.

  - ID: REL-SOURCE-PROOF-DUPLICATION
  - Status: open
  - Source story: spec-gh-30146368778-fix-tenants-release-startup-failure.md; surfaced by adversarial review 2026-07-25
  - Target artifact: references/Hexalith.Tenants/.github/workflows/release.yml, references/Hexalith.EventStore/.github/workflows/release.yml, references/Hexalith.Builds/Github/publish-containers/publication_preflight.py
  - Re-open trigger: when the bash and Python source proofs disagree, or when a third module copies the verify-source job.
  - Rationale: the verify-source job reimplements prove_current_green_source in bash, duplicated verbatim across Tenants and EventStore. The Python original additionally validates the workflow filename against WORKFLOW_PATTERN, rejects redirects via FailClosedRedirectHandler, and requires a positive integer run id; the bash copy relies on gh (which follows redirects) and hardcodes ci.yml in the URL. Two divergent implementations of the same gate will drift. Candidate fix: a shared composite action in Hexalith.Builds. Related: verify-source declares no timeout-minutes, so a wedged gh call inherits the 360-minute default and, with cancel-in-progress: false, blocks every subsequent release dispatch for six hours.

  - ID: REL-CI-TRUST-ANCHOR-UNPINNED
  - Status: open
  - Source story: spec-gh-30146368778-fix-tenants-release-startup-failure.md; surfaced by adversarial review 2026-07-25
  - Target artifact: references/Hexalith.Tenants/.github/workflows/ci.yml, references/Hexalith.Builds/.github/workflows/ci-cd-standards.md
  - Re-open trigger: any review of the release supply-chain threat model.
  - Rationale: the release path now demands byte-exact Hexalith.Builds identity (uses: pinned to 40-hex, validated against job.workflow_sha), but the CI run that verify-source accepts as proof of a green source is produced by ci.yml calling domain-ci.yml@main - a mutable reference. The evidence authorizing publication is generated by an unpinned workflow definition. ci-cd-standards.md currently sanctions @main for "routine, non-publication" Builds references, so changing this is a standards decision, not a module fix. Pre-existing and applies to every Hexalith module.


## Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-26)

Chunk 3a of 3 (never-reviewed tooling/CI plus the post-2026-07-21 review-patch delta: 13 paths,
2,219 diff lines, review manifest SHA-256
`1ee36f77ef4446741e545663c6e386122ce4c93653f7a4448a258cdc9594aac3`). Six review layers ran with
zero failed layers.

- **27.3-CR23 - chunk 3b unreviewed; fail-closed for `done`.** (Renumbered 2026-07-26 by code review, chunk 3b: this entry was minted as `27.3-CR7`, colliding with the existing `DW 27.3-CR7` create-story-verifier entry. Both were open and both were cited as `done` blockers, so the ID resolved to two unrelated obligations. Resolved 2026-07-26: chunk 3b has now been reviewed; this entry is closed by that review. **Renumbered again 2026-07-27 by code review, chunk 3:** a later, unrelated entry — the AC6/C2 production-deployment-verification red-run record — was independently minted as `27.3-CR17`, recreating the identical collision this entry was renumbered once already to escape. This entry, being resolved and historical, is renumbered to `27.3-CR18` rather than the active, currently-cited `27.3-CR17`. **Renumbered a third time 2026-07-27 by dev-story:** `27.3-CR18` was itself already taken by the resolved chunk-2 fail-closed-blockers entry above, so that renumber recreated the very collision it was performing. This entry moves to `27.3-CR23`, the first free ID; the pre-existing `27.3-CR18` above keeps the ID it held first. The recurrence is now bound by `CiTestInventoryTests.DeferredWorkRegister_RealRepo_DeclaresEachIdExactlyOnce`, which failed RED on this exact duplicate.) The eight governance/planning record
  - ID: 27.3-CR23
  - Status: resolved 2026-07-26 — chunk 3b reviewed; all in-scope chunks are now complete.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
  - Re-open trigger: resolved; a new chunk would need a new ID.
  - Evidence: the chunk-3b record in **Code Review Evidence** of the story file (8 governance/planning paths, 2,264 diff lines at HEAD `c9dfb06f`, manifest SHA-256 `605152e597357936680f5f171d9a87e09dfcb7887e21ecd953bfab6d550d6344`), which states that chunk 3b completes path-level review coverage.
  paths of Story 27.3 (story file, `epics.md`, `architecture.md`, the 2026-07-20 sprint change
  proposal, `deferred-work.md`, `sprint-status.yaml`, and the create-scope and adapter-profile
  evidence packets; 2,076 diff lines) have not been reviewed. Per `story-phase-ledger.md`, an
  intermediate chunk may emit findings but cannot finalize the ledger or synchronize completion
  status. Owner: Story 27.3 review owner. Consequence: the final `code-review` row cannot be
  appended and Story 27.3 cannot reach `done`. Reopen trigger: run code review over the chunk-3b
  path set, then append the final row carrying evidence that all three chunks are complete.
- **27.3-CR8 - test-double state store ships in the product container assembly.**
  - ID: 27.3-CR8
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3)
  - Target artifact: src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs
  - Re-open trigger: when the Story 27.2-origin structural move to a shared test-support project is scheduled.
  - Rationale: The class documents itself as a test double the runtime host does not register, yet it ships inside a project with `EnableContainer=true` and `ContainerRepository memories-access-telemetry`, reachable only through `InternalsVisibleTo`. A DI misregistration would satisfy `IAccessTelemetryStateStore` with no durability. Moving it to a shared test-support project is a Story 27.2-origin structural change. Owner: AccessTelemetry adapter owner. Consequence: a test double is present in the released image's assembly surface. Partially mitigated 2026-07-26 by dev-story: the adapter now validates `ttlInSeconds`, models the anti-resurrection conflict, prunes drained expiry minutes, and performs the same strong post-delete verification as the Dapr adapter, so a misregistration no longer silently discards expiry - but the structural placement is unchanged.
  `src/Hexalith.Memories.AccessTelemetry/Lifecycle/InMemoryAccessTelemetryStateStore.cs` documents
  itself as a deterministic adapter for lifecycle tests that the runtime host does not register,
  yet it lives in a project with `EnableContainer=true` and `ContainerRepository`
  `memories-access-telemetry`, reachable only via `InternalsVisibleTo` from the two test
  assemblies. It is non-durable, non-transactional, and discards `ttlInSeconds`, so a DI
  misregistration would satisfy `IAccessTelemetryStateStore` with no durability and no expiry.
  Owner: AccessTelemetry adapter owner. Consequence: a test double is present in the released
  image's assembly surface. Reopen trigger: move it to a shared test-support project, or add a
  guard asserting the runtime composition root never registers it. Pre-existing: introduced by
  Story 27.2, not by this chunk.
- **27.3-CR9 - commit `358bef35` bypasses the Conventional Commits contract.** Its subject carries
  - ID: 27.3-CR9
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3a/3)
  - Target artifact: .githooks / commitlint configuration
  - Re-open trigger: confirm the commit-msg hook rejects a missing type prefix, and decide whether the omitted product change needs a follow-up release note.
  - Rationale: Commit `358bef35` carries no Conventional Commits type prefix, and its body lists only the three new test files while omitting the `InMemoryAccessTelemetryStateStore.cs` purge-ordering product change in the same commit. It is already published on `main`, so correcting the message needs a history rewrite; the durable fix is the commit-msg gate, not this commit. Owner: repository workflow owner. Consequence: release semantics and the changed-surface audit trail are both wrong for that one commit.
  no type prefix and its body enumerates only the three new test files, omitting the
  `InMemoryAccessTelemetryStateStore.cs` purge-ordering product change in the same commit. The
  commit is already published on `main`, so correcting the message itself would require a history
  rewrite. Owner: repository workflow owner. Consequence: `feat`/`fix` release semantics and the
  changed-surface audit trail are both wrong for that commit. Reopen trigger: confirm the
  commit-msg hook rejects a missing type prefix, and record whether the omitted product change
  needs a follow-up release note.

## Deferred from: code review of 27-3-production-adapter-and-deployment-profile chunk 3b (2026-07-26)

### DW 27.3-CR10 - `sprint-status.yaml` has no executable integrity check

  - ID: 27.3-CR10
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3b/3)
  - Target artifact: _bmad-output/implementation-artifacts/sprint-status.yaml
  - Re-open trigger: a `development_status` key, status value, or `story_execution_order` entry drifts from `epics.md` without any check failing.
  - Rationale: `grep` over `tools/`, `tests/` and `.github/workflows/` finds no tool, test or workflow that parses `sprint-status.yaml`; the only test that opens it counts one `epic: 0` action block. Nothing validates duplicate `development_status` keys, allowed status values, `story_execution_order` membership against registered story keys, or `epics.md` parity, so every epic registration, story registration and status flip in the chunk-3b diff ships unchecked. This is a pre-existing repo-wide gap, not a defect introduced by Story 27.3; it is recorded here because Story 27.3's fail-closed status gates cite this file as their authority. Owner: repository tooling owner. Consequence: the sprint ledger every downstream gate treats as authoritative is entirely self-reported.

### DW 27.3-CR11 - Epic 21 retro action closed `done` behind a gate that has never fired

  - ID: 27.3-CR11
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3b/3)
  - Target artifact: _bmad-output/implementation-artifacts/sprint-status.yaml
  - Re-open trigger: the first fully-green `integration-fast` run either fires the required-surface gate successfully, or reveals the migration class was skipped while the gate stayed green.
  - Rationale: The action was flipped `open` -> `done` inside the chunk-3b diff. The closure is better evidenced than a first reading suggests: the five `EmbeddingVectorMigrationRedisIntegrationTests` facts did execute and pass in run 29798593273 (among 261 passed), which satisfies the action's "executes in an approved CI lane" criterion, and the note discloses its own caveats honestly. The residual is that the hardening cited in the same note has never run: `verify-integration-fast-coverage.py` asserts class presence rather than per-class outcome, its CI step inherits `success()` so it is skipped on red lanes, and run 29798593273 was itself red. The recorded reopen trigger for surface removal is human-detected only. Hardening is already tracked as open `21.10-A4-VERIFY`. Owner: Epic 21 / integration-lane owner. Consequence: a `done` retro action depends on an enforcement gate with no executed proof.

### DW 27.3-CR12 - `DW 27.3-CR3`'s reopen trigger cannot fire before the defect it prevents

  - ID: 27.3-CR12
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3b/3)
  - Target artifact: _bmad-output/implementation-artifacts/deferred-work.md
  - Re-open trigger: when `DW 27.3-CR3` is next touched, replace its trigger with an observable event (a charset guard, a contract assertion, or a test) rather than a restatement of the defect.
  - Rationale: `DW 27.3-CR3`'s reopen trigger reads "a `RecordId` containing `/` or other key-delimiter characters can reach `GetRecordKey`/`GetBucketKey`", but the entry's own rationale states it is unknown whether the record contract constrains `RecordId`, and no guard, test or gate observes that condition. The entry can therefore only reopen after the key collision it exists to prevent has already occurred. Owner: Story 27.3 adapter owner. Consequence: a deferred correctness item is effectively dormant.

### DW 27.3-CR13 - `epics.md` planning-convention drift for Epics 29-31

  - ID: 27.3-CR13
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3b/3)
  - Target artifact: _bmad-output/planning-artifacts/epics.md
  - Re-open trigger: before Story 30.1, 30.2 or 31.1 is set `ready-for-dev`.
  - Rationale: Stories 29.1, 29.2, 30.1, 30.2 and 31.1 each duplicate `**Status:** backlog` inside `epics.md` while Epic 27's stories carry none, creating two status authorities with nothing guarding them against drift. Separately, `epics.md` presents Epic 30 as Story 30.1 then Story 30.2 while `sprint-status.yaml:163` sequences `30-2` before `30-1` with a stated reason that Story 30.1's own activation gate never mentions, so a reader working from `epics.md` alone starts with the wrong story. Owner: the Epic 29/30 planning sessions. Consequence: story-selection order and status are ambiguous across two records.

### DW 27.3-CR14 - `maxConns` profile substitution needs a superseding course correction

  - ID: 27.3-CR14
  - Status: resolved 2026-07-27 - approved Sprint Change Proposal 2026-07-27 corrected the ADR immutable component block to `maxConns: "40"`, named `profile_sha256 dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14` and `mutation_manifest_sha256 2983ccdebedbd12e34bb1aec363335eb825301ce92d1c4ed87f8956d9c176b84` as the approved hashes, and identified `canonical_pg_onprem_profile()` in `tools/verify_access_telemetry_lifecycle.py` as the artifact carrying them. The 2026-07-20 proposal is superseded by reference and was not edited in place.
  - Evidence: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md; `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` component block and the appended profile-hash note; hash recomputed live from `canonical_pg_onprem_profile().manifest()` and pinned by `tests/tooling/access_telemetry_lifecycle/test_adapter_profile.py::AdapterProfileTests::test_canonical_pg_onprem_profile_hash_is_pinned`.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3b/3)
  - Target artifact: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-20-story-27-3-on-prem-postgresql-18-4.md
  - Re-open trigger: before Story 27.3 advances to done; an approved correction must pin `maxConns: "40"`, recompute `profile_sha256`, and name the artifact carrying the new hash.
  - Rationale: The shipped manifest is `"40"` while both the ADR immutable component block and the approved 2026-07-20 course correction pin `"64"`. Task 1 states any substitution changes the profile hash and requires another approved course correction. Administrator decided 2026-07-26 during code review that a new correction supersedes the 2026-07-20 pinning rather than amending an approved dated proposal in place. The profile ID string does not encode `maxConns`, so Task 1's substitution guard cannot detect the drift on its own. Owner: Story 27.3 adapter owner + Product Owner. Consequence: AC1's immutable profile and AC4's hash-bound approvals currently bind an object three records describe differently.

### DW 27.3-CR15 - deployment-verification lane needs an acceptance criterion and a named checkpoint

  - ID: 27.3-CR15
  - Status: resolved 2026-07-27 - approved Sprint Change Proposal 2026-07-27 added Story 27.3 acceptance criterion AC6, Task 2, and checkpoint C2 for the kind-based production-deployment-verification lane. C2 carries an accountable owner, a named CI-run evidence artifact, review state `pending`, and completion state `not complete`, and is explicitly independent of C1.
  - Evidence: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md; `_bmad-output/planning-artifacts/epics.md` Story 27.3 AC6; `_bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md` AC6, Task 2, and the C2 checkpoint row.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3b/3)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
  - Re-open trigger: before Story 27.3 advances to done; the lane must have a declared acceptance criterion and a checkpoint row carrying owner, evidence command, review state and completion state.
  - Rationale: The kind-based production-deployment-verification lane runs as a standalone `ci.yml` job with no dependency on the externally-blocked C1, and `epics.md:5137` assigns its three tools to Story 27.3, yet the story declares no acceptance criterion, no task and no checkpoint for it. The approved 2026-07-26 correction simultaneously freezes Story 27.3's acceptance criteria, so a checkpoint alone would prove an outcome no AC declares. Administrator decided 2026-07-26 during code review that a new correction adds the AC and its checkpoint, keeping the lane in Story 27.3. Owner: Story 27.3 owner + Product Owner. Consequence: an independently shipping lane is unaccountable to any acceptance criterion until the correction lands.

### DW 27.3-CR16 - Stories 30.1 and 31.1 must be split before selection

  - ID: 27.3-CR16
  - Status: resolved 2026-07-27 - approved Sprint Change Proposal 2026-07-27 split Story 30.1 into Stories 30.1, 30.3, 30.4 and 30.5, and Story 31.1 into Stories 31.1 and 31.2. Each new story carries one independently demonstrable outcome and an Implementation-evidence checkpoint requirement naming owner, evidence command or artifact, review state, and completion state. The external Hexalith.Builds activation gate narrowed to Stories 30.3, 30.4 and 30.5. No scope was added or dropped and no story status advanced; all remain `backlog`.
  - Evidence: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md; `_bmad-output/planning-artifacts/epics.md` Epic 30 and Epic 31 story definitions; `_bmad-output/implementation-artifacts/sprint-status.yaml` `development_status` rows and `story_execution_order` for `epic-30` and `epic-31`.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review, chunk 3b/3)
  - Target artifact: _bmad-output/planning-artifacts/epics.md
  - Re-open trigger: before Story 30.1, 30.2 or 31.1 is set `ready-for-dev`.
  - Rationale: Both stories created by the 2026-07-26 split reproduce the anti-template shape that split was executed to cure. Story 30.1 carries seven Given/When/Then blocks, names eight "separate reviewable checkpoints" with no owner, evidence command, review state or completion state, and gates the whole story on an unshipped external Hexalith.Builds revision — the same pattern that let independently shippable lanes accumulate under a blocked umbrella in Story 27.3. Story 31.1 bundles the OpenBao platform hardening and the runtime `secretstore` migration, two independently deployable outcomes, with no checkpoint table. `epics.md:555` and `story-scope-guard.md:30-31` do not bind while both are `backlog`, but selecting either as written re-creates the violation. Administrator decided 2026-07-26 during code review to split both now via correct-course. Owner: Product Owner. Consequence: without the split, Epic 30 and Epic 31 inherit Story 27.3's failure mode.

### DW 27.3-CR22 - gate C1.15 has no producer for four of its six named observations

  - ID: 27.3-CR22
  - Status: open
  - Source story: 27-21-runtime-and-control-plane-identity (registration transaction 2026-08-03; originated in Story 27.3 code review 2026-07-27)
  - Target artifact: tools/verify-access-telemetry-c1.ps1 and the C1.15 packet produced under `artifacts/access-telemetry-c1/C1.15`
  - Re-open trigger: before Story 27.21 checkpoint C1.15 can leave `not complete`; discharged only when its literal command captures every named observation from running `memories-access-telemetry` pods and an independent reviewer accepts that immutable packet. Producer existence or fixture success is not discharge.
  - Registration correction 2026-08-03: Story 27.21 now owns C1.15 and its literal producer reads the authenticated metadata API inside the lifecycle container, captures the explicit alpha pair separately, and fails closed on incomplete evidence. This entry stays open because no real running-target packet or independent gate review was produced by registration; Production lifecycle writes remain disabled. The historical measurement below is preserved as provenance.
  - Rationale: C1.15's named observation is composite - Dapr runtime version, sidecar image digest, Scheduler connections, actor types, enabled features and any alpha opt-in, captured from the running deployment rather than .NET package pins. The 2026-07-27 seventh `dev-story` invocation repaired the producer so it emits the first two: `daprd_version` and `daprd_build_info` are captured by probing `/daprd` (the official `ghcr.io/dapr/daprd` image places the binary there and populates no `PATH`), and `sidecar_image_digests` is bound to `status.containerStatuses[].imageID`. The remaining four have no producer at all. They are served by the Dapr sidecar metadata API (`GET /v1.0/metadata`), which is gated behind `DAPR_API_TOKEN` on this deployment (`DAPR_API_TOKEN_MODE=enabled`); reaching it from the evidence path would mean handling a runtime secret inside a tool whose output is a committed artifact, which was deliberately not done. Until then the gap was tracked only inside an append-only Debug Log paragraph with no owned entry, which this entry corrects. Owner: Deployment adapter owner. Consequence: gate C1.15 stays `pending`/`not complete` on its own evidence even though two of its six elements are now genuinely observed, so it cannot be cited as discharged; because C1.15 is an AC1 identity gate, this neither blocks nor advances any AC2 behavioural gate. **Measured 2026-07-27 by the eighth `dev-story` invocation, replacing the assertion above with observation.** Three token-free producers were probed against the live target and all three are unavailable, so no read-only path to the four remaining observations exists today: (1) the sidecar metadata API answers `HTTP/1.1 401 Unauthorized` without the token — `kubectl exec -n hexalith-memories memories-b667844cf-6s9j7 -c memories -- sh -c 'wget -qO- --timeout=5 http://127.0.0.1:3500/v1.0/metadata'`; (2) the unauthenticated daprd metrics endpoint on `:9090` exposes no `dapr_runtime_scheduler_*` and no actor-type series at all — its families are `dapr_component_*`, `dapr_grpc_io_*`, `dapr_http_*`, `go_*` and `process_*` only, confirmed by the same exec against `/metrics`; and (3) most decisively, the four observations are properties of the `PG-ONPREM-1` lifecycle workload, whose Deployments `memories-access-telemetry` and `memories-access-telemetry-clock` are both `0/0` — there is no pod to query even with a valid token. The two elements already captured come from the `memories` server pods, which share the control plane and the daprd image but not the lifecycle app id's actor types, scheduler connections or configuration. This entry is therefore gated on the same external prerequisite as C1 itself: it cannot be discharged before the lifecycle Deployments are scaled above zero, and the reopen trigger above should be read as requiring that first. No token was handled and no live mutation was made to obtain this.

### DW 27.3-CR17 - production-deployment-verification lane is red on the OpenBao runtime secret store

  - ID: 27.3-CR17
  - Status: open
  - Evidence: **Discharged by the entry's own re-open trigger condition:** GitHub Actions run `30405437576`, job `production-deployment-verification` (job ID `90429705579`) at commit `64434e574a68c0595e95bc4b6cf32166707ba321`, conclusion `success` — `Publish local release OCI archives` success, `Verify disposable production rollout` success, `Validate production deployment evidence` success, `Upload production deployment evidence` success, no step skipped, cancelled, or absent. The uploaded artifact (ID `8706483105`) records `verification-result.json` `status: succeeded` at terminal stage `required-server-mcp-restored` with the `secret-store-substitution.json` disclosure and full-body 503 fault evidence. Repaired inside Story 27.3's declared verification tooling per the Administrator's 2026-07-28 decision (verification-scoped `secretstores.kubernetes` substitution, commits `564d5d56` and `64434e57`); the OpenBao path itself remains unexercised by this lane and its proof remains Story 31.2 scope. Superseded failing citation follows. GitHub Actions run `30387272182`, job `production-deployment-verification` (job ID `90369590755`) at commit `a4517654e7993237c3bfba473fae6b6a027e3ad1`, conclusion `failure`; per-step outcomes `Publish local release OCI archives` success, `Verify disposable production rollout` failure, `Validate production deployment evidence` success, `Upload production deployment evidence` success, nothing skipped, cancelled, or absent. The uploaded artifact (ID `8699601794`, retrieved with `gh api repos/Hexalith/Hexalith.Memories/actions/artifacts/8699601794/zip` because `gh run download` fails on this artifact with a path-traversal error) records the identical fatal cause in `memories-54c694b68f-742gh-daprd-current.log` — `Secret "openbao-runtime-bootstrap" not found` — and additionally `Secret "openbao-access-telemetry-bootstrap" not found` in `memories-54c694b68f-dwp8s-daprd-current.log`: the second sidecar dies on `deploy/kubernetes/base/dapr/access-telemetry-secrets.yaml`, whose `secretstores.hashicorp.vault` backing was introduced by the same commit `4d2e4e2f` as `secretstore.yaml` and which is scoped to the `memories` app-id, so the Story 31.2 repair must provision or substitute both bootstrap Secrets, not only `openbao-runtime-bootstrap`. Re-derived and recorded 2026-07-28 by the ninth Story 27.3 `dev-story` invocation. Superseded citations, kept for provenance: run `30265014637` (job `89973572304`, commit `b073aa57`) was this entry's 2026-07-27 evidence; run `30246564974` (job `89914854458`, commit `fe19a27c`) was this entry's original evidence; and run `30263029678` failed earlier at archive publication for the unrelated `Hexalith.EventStore.Client` reason discharged by commit `3c24f8c2`. Corrected 2026-07-27 by code review (eighth-invocation review): the preceding phase appended the current run into `Rationale` without retiring the superseded run named here, leaving the entry citing two different runs as its evidence. Original detail follows. step `Verify disposable production rollout` failed at `tools/verify-production-deployment.ps1:359` with `[initial-server-health] memories did not report HTTP 200 aggregate Healthy within 60 seconds`; the uploaded `production-deployment-evidence` artifact records `level=fatal msg="Fatal error from runtime: failed to load components: rpc error: code = Unknown desc = Secret \"openbao-runtime-bootstrap\" not found"` in `memories-5b65756964-cxbxx-daprd-current.log`. **[Corrected 2026-07-30 by code review: the cited discharge artifact predates both `observedPostPatchTypes` and the schemaVersion 2 `observedComponents` record that the shipped validator now requires, so re-validating that exact packet would FAIL. The `resolved` status rests on evidence not reproducible against current tooling; a fresh qualifying run is owed. Owner: Story 27.3 owner. Reopen trigger: the next kind run that covers the 2026-07-29/07-30 verifier and validator patches.]** **[REOPENED 2026-07-31 by code review: `Status` was still `resolved 2026-07-28` while this very line recorded that re-validating the discharge artifact would FAIL and that a fresh qualifying run is owed. `Status` is the machine-read field, so every automated consumer saw a discharged entry; a narrative correction on the Evidence line does not reopen an entry. Prior value for provenance: `resolved 2026-07-28`. Discharge requires a kind run at or after the 2026-07-31 verifier/validator patches.]**
  - Source story: 27-3-production-adapter-and-deployment-profile (dev-story, Task 2 / checkpoint C2)
  - Target artifact: deploy/kubernetes/base/dapr/secretstore.yaml
  - Re-open trigger: **Split 2026-07-29 by code review (chunk 2).** This entry now covers ONLY the Story 27.3 / checkpoint C2 arm, discharged when `production-deployment-verification` reports `success` with no skipped render, apply, health, or evidence-validation step. The Story 31.2 arm - "before Story 31.2 is set `done`" - moved to `DW 27.3-CR29` and remains OPEN. The split is required because the run that discharged this entry succeeded by substituting the vault-typed stores at runtime, not by repairing this entry's own Target artifact; leaving both arms on one resolved entry pre-discharged Story 31.2's pre-`done` gate while `deploy/kubernetes/base/dapr/secretstore.yaml` is still unproven and `31-2-runtime-dapr-secret-store-migration` is still `ready-for-dev`.
  - Rationale: `deploy/kubernetes/base/dapr/secretstore.yaml` (`secretstores.hashicorp.vault`, commit `4d2e4e2f`) requires the `openbao-runtime-bootstrap` Kubernetes Secret and a reachable OpenBao at `https://hexalith-keys.openbao.svc.cluster.local:8200`. The disposable `kind` cluster the AC6 lane creates has neither, and `tools/verify-production-deployment.ps1` seeds six verification Secrets but not that one, so `daprd` exits at startup, the application's `dapr-sidecar` and `dapr-statestore` health checks stay `Unhealthy`, and aggregate `Healthy` is unreachable by construction rather than by timing. That component path was transferred to Story 31.1 by the approved 2026-07-26 Sprint Change Proposal (DW 27.3-CR6) and its runtime migration is Story 31.2, so the repair is theirs; the Administrator decided 2026-07-27 to record the blocker rather than repair it inside Story 27.3. Owner: **Story 31.2 (runtime Dapr `secretstore` migration).** **Reassigned 2026-07-28 by Story 31.1 second-pass code review, on the Administrator's decision.** The cause is the runtime `secretstore` migration and the absent `openbao-runtime-bootstrap` Secret, both Story 31.2 scope; Story 31.1 documents the platform and neither creates nor migrates that component. Story 31.1 is released as a blocking owner and its `done` is no longer gated on this entry. The register edit was explicitly authorized because the first review recorded this reassignment only inside Story 31.1's own resolutions table, leaving the enforcing artifact unchanged. Previously owned by: Story 31.1 and Story 31.2. Consequence: Story 27.3 checkpoint C2 stays `pending`/`not complete` and AC6 is unproven; because AC6 is independent of AC1-AC5, no C1 gate is blocked or advanced by this. **Evidence (re-derived 2026-07-27 by the seventh `dev-story` invocation, current reviewed source): GitHub Actions run `30265014637`, job `production-deployment-verification` (job ID `89973572304`) at commit `b073aa577ad3006300a5d7192392bb0ca656944b`, conclusion `failure`. Per-step outcomes: `Publish local release OCI archives` **success**, `Verify disposable production rollout` **failure**, `Validate production deployment evidence` **success**, `Upload production deployment evidence` **success** - nothing skipped, cancelled, or absent. The intervening `Hexalith.EventStore.Client` publication blocker recorded at run `30263029678` is discharged by commit `3c24f8c2` ("consume published EventStore package catalog"): archive publication now succeeds and the rollout step runs, so this entry's OpenBao cause is once again the sole reason the lane is red. The uploaded artifact's `production-deployment-verification/memories-6799676796-4m4vn-daprd-current.log` records the identical `level=fatal msg="Fatal error from runtime: failed to load components: rpc error: code = Unknown desc = Secret \"openbao-runtime-bootstrap\" not found"`, and the terminal `health-initial-server-health-029.json` records `503` with `dapr-sidecar` and `dapr-statestore` `Unhealthy` while `redis-ping` and `redisearch` are `Healthy`. Nothing about this entry's owner, consequence, or re-open trigger changes.** **Verification-lane repair applied 2026-07-28 by the ninth Story 27.3 `dev-story` invocation, on the Administrator's decision of the same date (the "substitutes a verification-scoped secret store" arm of this entry's own re-open trigger).** Diagnosis first: seeding the two bootstrap Secrets alone is insufficient, because `statestore`, `pubsub`, and `conversation-openai` carry `auth.secretStore: secretstore`, so the Redis state store's own init issues a live vault read against the OpenBao the disposable cluster does not have (verified: dapr 1.18.1 `secretstores.hashicorp.vault` Init builds its client without contacting the server — proven with a local standalone daprd loading the exact component against an unreachable address — but consumer-component secretKeyRef resolution does contact it). The applied repair: `tools/verify-production-deployment.ps1` applies the rendered production manifests verbatim, then, while the applications are scaled to zero, merge-patches Components `secretstore` and `access-telemetry-secrets` to verification-scoped `secretstores.kubernetes` aliases (names and scopes preserved by the cluster object), so the identical secretKeyRef name/key pairs (`redis-secret/password`, `llm-secret/OPENAI_API_KEY`) resolve from the already-seeded verification Secrets under the existing RBAC allow-lists. The substitution cannot happen silently: the verifier writes `secret-store-substitution.json` and `tools/validate-production-deployment-evidence.ps1` fails any packet whose disclosure is missing or incomplete (RED-proven, mutation-proven both directions; `tests/tooling/production_deployment_evidence` 30 -> 33 cases, 33/0/0). The OpenBao path itself remains unexercised by this lane and its proof remains Story 31.2 scope — this entry's Story 31.2 ownership of the runtime `secretstore` migration is unchanged, but checkpoint C2 no longer waits on it. Discharge still follows the re-open trigger above: a `production-deployment-verification` run reporting `success` with no skipped render, apply, health, or evidence-validation step. **[Corrected 2026-07-30 by code review: the "scaled to zero" safety claim in this sentence is FALSE and is preserved only as append-only history. `kubectl apply -f` creates deployment/memories and deployment/memories-mcp at their manifest replica count, and the scale-to-zero happens AFTER the substitution block - the qualifying run shows apply at 22:45:22.193 and substitution at 22:45:22.396. Consumer pods can start against the vault-typed store inside that sub-second window and crash-loop at component load. The window cannot be closed by reordering, because the pods are created by the apply itself. The verifier comment and the tooling test were corrected on 2026-07-29; these narrative copies were missed by that item.]** **[Second correction, 2026-07-30 by code review - validator-behaviour claim. This sentence was made FALSE by the 2026-07-29 chunk-2 change, which gated the entire disclosure check on a succeeded run AND the file's presence, so a missing disclosure was merely reported on stdout and the packet validated. The register was documenting a fail-closed gate that no longer existed. It is TRUE again as of 2026-07-30: the verifier always writes the record (substitutionPerformed=false when nothing was substituted) and the validator requires and shape-checks it on every succeeded run, per-component. Mutation-proven in both directions: removing the required-file check, the membership check, the per-component type check, the false-claim contradiction check, and the failed-run guard are each caught by a distinct test. The cited `30 -> 33 cases, 33/0/0` figure is superseded; the lane now discovers 67.]**


## Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-27)

**Restructured 2026-07-27 by code review (eighth-invocation review).** These three items
were originally filed as plain bullets carrying none of the register's fields, so
`read_deferred_entries` returned nothing for them and no `done` gate - including the
ID-uniqueness guard shipped in the same commit - could observe them. They are re-filed
below as structured entries. Two are discharged by that same review pass.

### DW 27.3-CR25 - the register parser does not recognize its own documented `Evidence` field

  - ID: 27.3-CR25
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-27)
  - Target artifact: tools/verify-integration-stub-closure.py
  - Re-open trigger: if `DEFERRED_FIELD_RE` and the C# `DeferredConsumerFieldRegex` mirror in `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` ever diverge on the field vocabulary again.
  - Rationale: `Evidence` was absent from `DEFERRED_FIELD_RE` although this register documents it as required ("One of `Evidence:` or `Rationale:`") and roughly twenty entries use it. An unrecognized `  - Evidence: ...` line starts with two spaces, so `read_deferred_entries`'s continuation branch folded it into the preceding field - usually `status`, which then parsed as `open - Evidence: GitHub Actions run ...` and could never equal `accepted` at the closure check on line 337. The original bullet stated this defect but overstated it as "the register has no recognized `Evidence` field at all", which is false: this file documents it and the C# reader honours it; only the Python consumer omitted it. Owner: Story 30.1 (it owns the CI/test-lane wiring for this tool; see `DW 27.3-CR24`). **Attempted and reverted 2026-07-27 by code review (eighth-invocation review).** The one-word fix was applied and verified - adding `Evidence` to the alternation made `27.3-CR17`'s `status` parse as `open` instead of `open - Evidence: GitHub Actions run ...`, with the `integration_stub_closure` lane still 7/7 - and was then reverted, because `tools/verify-integration-stub-closure.py` is not in Story 27.3's `## File Scope` allow-list and `tools/check-story-file-scope.py` exits `1` on it. Widening a fail-closed scope gate is a scope decision, not a review patch. Unblock with either a `Scope-Override: tools/verify-integration-stub-closure.py - register parser field vocabulary repair` line or by landing the fix under its owning story. Consequence: every entry using the documented `Evidence:` field still has its preceding field corrupted by the fold, so `status` can never equal `accepted` for those entries at the closure check.

### DW 27.3-CR26 - `Historical Context Classification` was never re-derived for the narrowed story

  - ID: 27.3-CR26
  - Status: resolved
  - Evidence: Resolved 2026-08-01 by approved Sprint Change Proposal 2026-08-01. All 24 prior references were re-evaluated against the retained C0/C2/C3/C4 outcome, their classifications and permitted influences were rewritten, and the approving 2026-08-01 proposal was added as the 25th row. The inherited anti-template table and disclaimer were replaced.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-27)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
  - Re-open trigger: if Story 27.3 scope changes again or a later artifact copies rather than re-derives its classification; the current discharge is the 25-row re-derived record in the Story 27.3 artifact.
  - Rationale: The table is an unrevised byte-for-byte copy of the superseded pre-split story's table and was re-derived at neither the 2026-07-20 nor the 2026-07-26 split. The story discloses this itself, and the finding demanding re-derivation was closed on two added rows plus a disclaimer rather than on a fresh classification pass. The source artifact is itself classified `anti-template` on three simultaneous triggers, so `story-scope-guard.md:35-36` requires the record to be produced for *this* story rather than inherited. The prior review closed it "deferred, pre-existing", which `story-scope-guard.md:50` forbids for a confirmed violation. Owner: Story 27.3 story owner (the classification is a human judgement over current epic intent, not a mechanical repair). Consequence: the guard-mandated provenance record for this story is unverified, so no reader can tell which historical influences were actually re-checked for the narrowed scope. **Resolved record 2026-08-01:** the required human re-derivation is now present; this entry is not closed as deferred or pre-existing.

### DW 27.3-CR27 - Task 2 subtask cited a superseded CI run

  - ID: 27.3-CR27
  - Status: resolved 2026-07-27
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-27)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
  - Re-open trigger: if a later `production-deployment-verification` run supersedes `30265014637` as the current four-archive producer-attribution evidence.
  - Rationale: The subtask verified four-archive producer attribution against run `30246564974`; publication subsequently failed at run `30263029678` and returned to `success` only at run `30265014637`. The subtask sits in human-owned task definition, outside both `dev-story`'s and `code-review`'s permitted sections, so the prior review closed it "deferred, pre-existing" - valid for *who* repairs it, not for closing the finding. Owner: Story 27.3 story owner. Consequence: none remaining - the Administrator granted `code-review` a one-time authorization on 2026-07-27 confined to this run citation, and the subtask now cites run `30265014637`.
  - Evidence: Story 27.3 Task 2's fourth subtask now cites GitHub Actions run `30265014637`, job `production-deployment-verification` (job ID `89973572304`), whose `Publish local release OCI archives` step reports `success` with all four archives present; the superseded runs `30246564974` and `30263029678` are named in the subtask as provenance. Placed after `Rationale` deliberately: `DEFERRED_FIELD_RE` in `tools/verify-integration-stub-closure.py` does not recognize `Evidence`, so this line is folded into the preceding field - putting it last means it lands in `rationale` rather than corrupting the load-bearing `status`, until `DW 27.3-CR25` is discharged.

## Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-27, eighth-invocation review)

### DW 27.3-CR24 - the deferred-work status verifier is wired to nothing

  - ID: 27.3-CR24
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-27, eighth-invocation review)
  - Target artifact: tools/verify-integration-stub-closure.py
  - Re-open trigger: before Story 27.3 is set `done`; discharged when `tools/verify-integration-stub-closure.py` is invoked by a CI workflow, script, or test-lane entry other than its own unit test, so that its `status`/`Owner`/`Re-open trigger` checks actually gate something.
  - Rationale: Repo-wide search finds exactly one caller of `tools/verify-integration-stub-closure.py` - its own unit test `tests/tooling/integration_stub_closure/`. No workflow, script, or tool invokes it, so the `status == accepted`, `Owner:` and `Re-open trigger` enforcement at `verify-integration-stub-closure.py:330-345` gates nothing today. This matters because several 2026-07-27 review decisions on this story were argued from that verifier's parsing behaviour - notably the patch that folded a `DW 27.3-CR17` `Evidence:` bullet into `Rationale` to satisfy its `DEFERRED_FIELD_RE`. A parser that gates nothing is a weak basis for a record change, and the same range shipped a C# guard (`CiTestInventoryTests.DeferredWorkRegister_RealRepo_DeclaresEachIdExactlyOnce`) whose lexical rules disagree with it in both directions. Pre-existing and not caused by this change. Owner: Story 30.1 (CI/test-lane wiring). Consequence: register-integrity checks that reviews rely on are advisory only, so a corrupted `status` or a missing `Re-open trigger` reaches `done` unobserved.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The readiness gate does not require completion dates for evidence rows at review or done.
  evidence: `check_evidence_rows` reads only the status-column index, while the phase-ledger policy requires completed evidence to carry a date and forbids dateless rows at done.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The readiness gate accepts a bare blocked evidence status without accountability metadata.
  evidence: Any non-pending value bypasses C6, so `blocked` passes without the policy-required owner, consequence, and reopen trigger.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The readiness gate accepts unknown evidence statuses as completed.
  evidence: C6 rejects only values in `PENDING_CELLS`, so a typo or invented state such as `gibberish` passes without a recognized completed-state vocabulary.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The readiness gate silently ignores evidence rows whose review-status cell is absent.
  evidence: `check_evidence_rows` continues when `status_index` exceeds the row length instead of reporting the missing status required by policy.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Readiness File List reconciliation checks changed-but-unlisted paths but not listed-but-unchanged paths.
  evidence: C1 builds only an `unlisted` set even though the phase-ledger policy requires the cumulative File List and changed set to contain identical entries.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Readiness File List entries are treated as globs instead of exact historical paths.
  evidence: C1 calls `matches_glob` for File List entries, allowing a broad entry such as `src/**` to replace the policy-required path-level inventory.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Cumulative readiness diff collection loses the source path of renames.
  evidence: `derive_cumulative_changed` uses `git diff --name-only`, while policy requires a rename entry to identify both old and new paths.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: A review or done artifact can pass C2 without a create-story ledger row.
  evidence: `check_ledger` requires `dev-story` for review/done and `code-review` for done but never requires the creation baseline mandated by policy.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The newest reconciliation cell accepts unstructured blocker substrings.
  evidence: C2 accepts any cell containing markers such as `not run` or `blocker` without validating the required command, owner, consequence, and reopen trigger.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: A review artifact can carry governed File List or evidence data but no phase ledger and still pass.
  evidence: When `find_ledger` returns none, validation emits a skipped note instead of enforcing the ledger required at review or done.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: An in-progress or review artifact can omit its File List and still pass readiness.
  evidence: When `parse_section_paths` returns none, validation emits a skipped note instead of enforcing the core cumulative completeness input.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The readiness gate does not enforce chronological order of canonical ledger phases.
  evidence: `check_ledger` records phase presence but never compares canonical phase indices, so an impossible lifecycle order can pass.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Canonical ledger rows can retain placeholder Date or Change cells.
  evidence: C2 checks placeholders only in Test count and File List reconciliation despite the canonical five-column record requiring Date and Change too.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: A whitespace-only exclusion owner is accepted by the readiness parser.
  evidence: `parse_exclusions` checks only that the owner regex matched and strips the captured value without rejecting an empty result.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Readiness hook and CI adoption lack an executable entry-point contract test.
  evidence: Existing tests exercise the CLI and policy prose but do not pin the hook's `--derive-cumulative` invocation or CI's prepared changed-file invocation.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The local commitlint hook fails open when repository dependencies are absent.
  evidence: `.githooks/commit-msg` prints an installation hint but exits successfully when `node_modules/.bin/commitlint` is unavailable, contrary to mandatory local validation policy.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Commitlint default ignores allow merge- and version-shaped messages through the stated every-commit policy.
  evidence: `commitlint.config.mjs` does not disable default ignores, so commitlint's built-in ignored message shapes are outside the configured Conventional Commit rules.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Main-push commitlint runs can cancel an earlier range before it is validated.
  evidence: The workflow groups all main pushes together with `cancel-in-progress: true`, while each reusable run validates only its event-specific before-to-after range.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Contributor guidance still recommends the forbidden chore branch and commit type.
  evidence: `CONTRIBUTING.md` contains `chore/<short-name>` and `chore:` examples while the shared baseline and current commitlint type enum forbid `chore`.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The dirty commitlint policy and PR-title workflow changes lack negative behavioral verification.
  evidence: Repository tests do not execute the hook against an invalid message, reject forbidden message shapes through the pinned config, or contract-test PR-title edit wiring.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Plain internal OperationCanceledException timeout mapping in the Tenants REST client lacks regression tests.
  evidence: The new header- and body-phase catches handle `OperationCanceledException`, but tests throw only the narrower `TaskCanceledException` unless caller cancellation is requested.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Tenants invalid-cursor transport mapping is not integrated with gateway retry coverage.
  evidence: REST-client tests stop at the `InvalidCursor` enum and gateway tests inject an already mapped `invalid-cursor` exception, leaving `ToReasonCode` disconnected from page-one recovery tests.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Independent tenant-detail and member read fault containment lacks page-level verification.
  evidence: The page now maps each faulted initial read to its own unavailable state, but existing tests cover pending success and cancellation rather than either read faulting independently.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The Tenants member paging state machine has only happy-path coverage.
  evidence: Existing tests do not verify retained previous data, invalid-cursor recovery, failed-page state preservation, the 50-entry history cap, or navigation from an empty later page.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: Failed refresh-subscription retry and duplicate-subscription protection lack verification.
  evidence: Tests do not assert `Empty.IsSubscribed` is false followed by successful retry, nor exercise the audit page's in-flight setup guard under overlapping parameter passes.

- source_spec: `_bmad-output/implementation-artifacts/spec-resolve-story-gate-commit-path.md`
  summary: The Tenants command-side URI scheme gate lacks non-HTTP composition tests.
  evidence: Existing malformed-scheme theories vary only `Tenants:BaseAddress` while retaining a valid EventStore address, so command-gateway fallback is not pinned.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-30423676094-access-telemetry-coverage-collector.md`
  summary: Contributor coverage guidance still describes six projects although the authoritative Docker-free inventory contains seven.
  evidence: The mismatch predates this fix; `tests/README.md` and `CONTRIBUTING.md` omit AccessTelemetry while `tools/test-projects.unit-contract.txt` and `requiredReportProjects` include it.

## Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-29), chunk 1 of 3

### DW 27.3-CR41 - the default-branch story-readiness check has no governed changed set

  - ID: 27.3-CR41
  - Status: open
  - Evidence: `python3 tools/check-story-review-readiness.py --story-key 27-3-production-adapter-and-deployment-profile` exits `1` on committed `main` with `C1: the changed set is empty for a governed story`.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 1 of 3)
  - Target artifact: tools/check-story-review-readiness.py
  - Re-open trigger: before Story 27.3 is set `done`; discharged by a passing default-branch invocation over the governed changed set or an accepted blocker recorded under the story-readiness policy.
  - Rationale: The concurrent `spec-resolve-story-gate-commit-path` work owns this gate defect. Owner: that specification's implementation owner. Consequence: Story 27.3 remains fail-closed for `done` because its default-branch readiness invocation cannot prove the governed change.

### DW 27.3-CR42 - the portable purge checkpoint does not overlap purge and writes

  - ID: 27.3-CR42
  - Status: resolved 2026-08-01
  - Evidence: The historical gap was that Story 27.2's `FiveHundredComponentOperationsWhilePurgeRuns_PreserveNewerRecordsAndAtomicPairs` started purge before creating the write tasks. After independent-review remediation, the coordinated state-store decorator holds the purge `GetDueEntriesAsync` call active until an actual inner live `WriteRecordAndIndexAsync` commit completes. The test captures all 500 live records and exact expiry-entry values, proves the seeded due record is absent, proves every captured live record equals the durable record, proves the retained expiry-entry set equals the exact 500 captured live entries, and proves all 501 writes committed exactly three atomic operations. Exact focused command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests.FiveHundredComponentOperationsWhilePurgeRuns_PreserveNewerRecordsAndAtomicPairs -parallel none -noLogo`; current post-second-review result: exit `0`, `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.214s`). C0 remains open pending targeted independent acceptance.
  - Final-review rejection: reopened 2026-08-01. The cited decorator kept its outer `GetDueEntriesAsync` active only after `inner.GetDueEntriesAsync` had already completed, and did not set the write-overlap flag until after the inner write completed. The prior green result therefore does not discharge CR42. C0 remains open while the operation-boundary proof is corrected and freshly executed.
  - Final-review remediation: resolved 2026-08-01 on fresh executed evidence. `InnerOperationOverlapStateStore` is now the actual `inner` called by `CoordinatedAccessTelemetryStateStore`. Its one-shot due-read and write methods each record entry and await the counterpart before either can delegate or complete; `OverlapObserved` is set only when both entry signals are complete and the participating-operation completion count is still zero. The test asserts both inner entries and that invariant before retaining the exact 500 survivors, seeded-due absence, exact 500 expiry entries, and all 501 three-operation writes. The clean lane's exact focused command above exited `0` with `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.227s`). C0 remains open pending independent re-acceptance and a reviewer-owned fresh execution.
  - Final independent acceptance: accepted 2026-08-01 after source inspection and reviewer-owned clean execution. Fresh Release build: `0 Warning(s)`, `0 Error(s)` in `64.77s`; exact discovery: eight methods; CR42 focused result: `1/1` in `0.406s`; canonical result: `8/8` in `0.556s`; all error/failure/skip/not-run counts zero. Exact 14-path readiness and diff hygiene passed. The reviewer accepted the operation-entry proof and authorized C0 closure in Story 27.3.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 1 of 3)
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs
  - Re-open trigger: before the Story 27.2 portable checkpoint is cited as purge/write concurrency evidence; discharged when an executed test forces both operations to overlap and proves the retained-record and atomic-pair assertions under that overlap.
  - Rationale: Owner: Story 27.2 lifecycle checkpoint owner. Consequence: the current C0 handoff citation proves purge-then-write ordering, not concurrent purge/write safety.

### DW 27.3-CR43 - the admission checkpoint measures capacity rather than 250 events per second

  - ID: 27.3-CR43
  - Status: resolved 2026-08-01
  - Evidence: The historical gap was that Story 27.2's `AdmissionAt250EventsPerSecond_IsByteBoundedAndDropsNewestAtQueueFull` performed 500 immediate sequential enqueues. After independent-review remediation, an asynchronous producer waits on a `PeriodicTimer` backed by `FakeTimeProvider`; per-attempt ready/completed/acknowledged handshakes require exactly one attempt at every four-millisecond tick and make coalescing or an incorrect rate fail. The record limit is 500 while the byte limit is exactly 250 canonical records, independently binding byte capacity; exactly 250 attempts occur in each trusted-time second, the first 250 remain, and the newest 250 are rejected as `QueueFull`. Exact focused command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests.AdmissionAt250EventsPerSecond_IsByteBoundedAndDropsNewestAtQueueFull -parallel none -noLogo`; current post-second-review result: exit `0`, `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.228s`). C0 remains open pending targeted independent acceptance.
  - Post-CR42-remediation regression evidence: the fresh clean lane reran the exact focused command above and exited `0` with `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.253s`). C0 remains open pending independent re-acceptance.
  - Final independent acceptance: accepted 2026-08-01 on the reviewer-owned fresh lane; the focused result was `1/1` in `0.378s`, the canonical result was `8/8` in `0.556s`, and every error/failure/skip/not-run count was zero. C0 closure is recorded by Story 27.3.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 1 of 3)
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs
  - Re-open trigger: before the checkpoint is cited as throughput evidence; discharged when an executed test measures admission at the declared 250-events/s rate while preserving the byte-bound and drop-newest assertions.
  - Rationale: Owner: Story 27.2 lifecycle checkpoint owner. Consequence: the existing method proves capacity/drop behavior but cannot support the rate claim in the C0 handoff.

### DW 27.3-CR44 - the two-writer checkpoint is sequential and single-process

  - ID: 27.3-CR44
  - Status: resolved 2026-08-01
  - Evidence: The historical gap was that Story 27.2's `TwoServerWriters_ProduceUniqueRecordsWithoutCrossTenantMarkerMixupOrRawStorage` invoked two sanitizer instances sequentially. After two independent-review remediations, the portable test retains two independent fake clocks, generators, queues, lifecycle processors, workers, and delivery-client boundary contexts, coordinates overlap at the shared state-store `WriteRecordAndIndexAsync` seam, proves the same tenant/user with the same marker key produces identical markers across both writers, and separately proves different tenants/users remain isolated. Durable records retain their owning markers, both persists use exact three-operation atomic writes, and canonical storage contains no raw tenant, user, or query data. These are independent same-process boundary contexts, not literal operating-system processes. Exact focused command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests.TwoServerWriters_ProduceUniqueRecordsWithoutCrossTenantMarkerMixupOrRawStorage -parallel none -noLogo`; current result: exit `0`, `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.207s`). C0 remains open pending targeted independent acceptance.
  - Post-CR42-remediation regression evidence: the fresh clean lane reran the exact focused command above and exited `0` with `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.223s`). `AccessTelemetryOperationRendezvous` remains CR44-only and was not changed. C0 remains open pending independent re-acceptance.
  - Final independent acceptance: accepted 2026-08-01 on the reviewer-owned fresh lane; the focused result was `1/1` in `0.352s`, the canonical result was `8/8` in `0.556s`, and every error/failure/skip/not-run count was zero. The reviewer confirmed the CR44-only rendezvous attribution. C0 closure is recorded by Story 27.3.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 1 of 3)
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs
  - Re-open trigger: before the method is cited as independent two-server concurrency evidence; discharged by an executed test using independent writer clocks/process boundaries and overlapping writes.
  - Rationale: Owner: Story 27.2 lifecycle checkpoint owner. Consequence: the current C0 handoff does not prove the independent-writer behavior its method name implies.

### DW 27.3-CR45 - the outage checkpoint does not model a sixty-second outage

  - ID: 27.3-CR45
  - Status: resolved 2026-08-01
  - Evidence: The historical gap was that Story 27.2's `TemporarySixtySecondOutage_RecoversAndFiveMinuteRetryAgeStopsOldWork` scripted one failure followed by success. After two independent-review remediations, one fully populated Enabled Development `AccessTelemetryOptions` instance validates successfully before the real worker is constructed, with both retry delays at the legal five-second maximum. Each post-attempt assertion waits for the next observed worker timer, closing the attempt/ack race; all T+0 through T+55 attempts retain and resend the original record ID, the T+60 success sends that same record, and every timer requests exact five-second/infinite-period scheduling. A record at retry age 4:59.999 remains and delivers; a record at exactly five minutes drops without another attempt. Exact focused command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests.TemporarySixtySecondOutage_RecoversAndFiveMinuteRetryAgeStopsOldWork -parallel none -noLogo`; current result: exit `0`, `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.239s`). C0 remains open pending targeted independent acceptance.
  - Post-CR42-remediation regression evidence: the fresh clean lane reran the exact focused command above and exited `0` with `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.265s`). C0 remains open pending independent re-acceptance.
  - Final independent acceptance: accepted 2026-08-01 on the reviewer-owned fresh lane; the focused result was `1/1` in `0.354s`, the canonical result was `8/8` in `0.556s`, and every error/failure/skip/not-run count was zero. C0 closure is recorded by Story 27.3.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 1 of 3)
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs
  - Re-open trigger: before the method is cited as outage-duration or retry-scheduling evidence; discharged when the dependency remains unavailable for a measured sixty seconds and retry scheduling is asserted against trusted time.
  - Rationale: Owner: Story 27.2 lifecycle checkpoint owner. Consequence: the current C0 handoff proves recovery after one scripted failure, not the named outage duration or schedule.

### DW 27.3-CR46 - the least-privilege checkpoint parses YAML as formatting-sensitive text

  - ID: 27.3-CR46
  - Status: resolved 2026-08-01
  - Evidence: The historical gap was that Story 27.2's Kubernetes least-privilege checkpoint used substring assertions. After two independent-review remediations, a reordered and reserialized semantic roundtrip passes, while the node-tree validator pins exact API versions, kinds, metadata names, access-control defaults/trust domains, policy namespaces/trust domains/defaults, four normalized grants, Component type/version/`initTimeout: 1m`/auth store, and lifecycle-only scope. Positive parsing remains formatting/order independent. Negative mutations cover non-mapping roots; wrong and missing authoritative fields; wildcard, duplicate, extra, and missing policies/grants/verbs/scopes; an added third identity; and malformed YAML. Exact focused command: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.IntegrationTests/bin/Release/net10.0/Hexalith.Memories.IntegrationTests.dll -method Hexalith.Memories.IntegrationTests.Telemetry.AccessTelemetryLifecycleIntegrationCheckpointTests.KubernetesDaprComponentScopes_AreExplicitAndLeastPrivilege -parallel none -noLogo`; current result: exit `0`, `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.218s`). C0 remains open pending targeted independent acceptance.
  - Post-CR42-remediation regression evidence: the fresh clean lane reran the exact focused command above and exited `0` with `Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0` (`Time: 0.243s`). C0 remains open pending independent re-acceptance.
  - Final independent acceptance: accepted 2026-08-01 on the reviewer-owned fresh lane; the focused result was `1/1` in `0.252s`, the canonical result was `8/8` in `0.556s`, and every error/failure/skip/not-run count was zero. C0 closure is recorded by Story 27.3.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 1 of 3)
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Telemetry/AccessTelemetryLifecycleIntegrationCheckpointTests.cs
  - Re-open trigger: before the method is cited as authorization-boundary evidence; discharged when structure-aware YAML assertions prove the exact application identities, operations, verbs, and negative cases independently of formatting.
  - Rationale: Owner: Story 27.2 lifecycle checkpoint owner. Consequence: formatting or a longer identifier can satisfy the current checks without proving the intended least-privilege policy.

## Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-29)

Chunk 2 of 3 (production-deployment verification tooling; commits `564d5d56` and `64434e57`). Two
findings were deferred as pre-existing. The readiness-gate defect is **not** re-filed here: it is
already recorded as a prose bullet in the 2026-07-29 chunk-1 block above, and converting that bullet
to structured form is the open `[Review][Patch]` item at story line 437. Filing a second record for
it would repeat the duplicate-ID defect this register already caught as `DW 27.3-CR18`.

### DW 27.3-CR28 - the chunk-1 remediation re-review left 15 findings open and unapplied

  - ID: 27.3-CR28
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 2 of 3)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md
  - Re-open trigger: before Story 27.3 leaves `in-progress`; discharged when all 15 items are checked off or explicitly re-routed, and the 3-chunk 2026-07-29 review records chunk-completeness for chunks 1, 2 and 3.
  - Rationale: the 2026-07-29 re-review of the chunk-1 remediation delta filed 2 `[Review][Decision]` items (story lines 427-428) and 13 `[Review][Patch]` items (story lines 430-442); all 15 remain `- [ ]` at HEAD and no remediation delta exists in the working tree. They belong to chunk 1's own remediation cycle, not to chunk 2, so this chunk defers rather than re-files them. Three were independently re-derived and confirmed correct by this chunk: the AccessTelemetry lane equation is stale at `0 + 31 + 39 = 70` while live discovery is 78 methods (`11da726f171f23216bed649a7a6b558d856f0da7cac211272b517667ce399d51`) and the correct equation is `0 create + 37 Story 27.3 + 41 external = 78`; the 2026-07-27 `+0` row regresses `access_telemetry_lifecycle` `14 -> 13` and `production_deployment_evidence` `30 -> 27`, which no `+0` delta can produce; and the four retroactive `correct-course` adoption rows carry current totals but no phase delta and no blocker tuple, although `+0` is derivable for a record-only phase. Owner: Story 27.3 (dev-story, as a review continuation). Consequence: `done` stays fail-closed while any of the 15 is open, and the final `code-review` ledger row cannot be appended until every chunk of the 2026-07-29 review is complete.

### DW 27.3-CR29 - the OpenBao runtime secret store is still unproven by any executed lane

  - ID: 27.3-CR29
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 2 of 3)
  - Target artifact: deploy/kubernetes/base/dapr/secretstore.yaml
  - Re-open trigger: before Story 31.2 is set `done`; discharged when an executed lane loads `secretstore` and `access-telemetry-secrets` at their production `secretstores.hashicorp.vault` type against a reachable OpenBao and resolves their consumers' secretKeyRefs.
  - Rationale: split out of `DW 27.3-CR17` on 2026-07-29 by code review. `DW 27.3-CR17` was discharged by run `30405437576`, but that run went green because `tools/verify-production-deployment.ps1` rewrites both stores to `secretstores.kubernetes` after the verbatim apply - it routes around the Target artifact rather than repairing it. Closing both arms on that run left Story 31.2 free to reach `done` with nothing forcing an OpenBao-path verification. Owner: **Story 31.2 (runtime Dapr `secretstore` migration).** Consequence: the production secret-resolution path ships unexercised; the AC6 lane proves a cluster whose secret stores are not the ones the manifests declare.

### DW 27.3-CR30 - no lane exercises the vault secret-resolution path at runtime

  - ID: 27.3-CR30
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-29, chunk 2 of 3)
  - Target artifact: tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs
  - Re-open trigger: before Story 27.3 checkpoint C2 is cited as evidence for any claim about secret resolution, and before Story 31.2 is set `done`; discharged when one lane loads a `secretstores.hashicorp.vault` component and resolves a secretKeyRef through it.
  - Rationale: recorded 2026-07-29 by code review (chunk 2). AC8's static lane asserts `type: secretstores.hashicorp.vault` for both components against the rendered manifests - which remains true, since the substitution is runtime-only - while AC6's runtime lane now guarantees that type is never the one running. The two lanes are individually correct and jointly leave the vault path unexercised. This is a coverage hole, not a contradiction, and with `DW 27.3-CR17` closed no open entry stated it. Owner: Story 31.2, jointly with whoever next revises AC8's static assertions. Consequence: a regression in vault-typed secret resolution is invisible to both lanes.

## Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-30)

- Chunked review is incomplete. Chunk 3 (governance/planning records) of the 2026-07-29 three-chunk review has not started, and 15 chunk-1 findings remain unchecked (2 `[Review][Decision]`, 13 `[Review][Patch]`). Already tracked as `DW 27.3-CR28` with the reopen trigger "before Story 27.3 leaves `in-progress`"; recorded here because `story-phase-ledger.md` makes it a fail-closed blocker — an intermediate chunk cannot finalize the ledger or set `done`. Owner: Story 27.3 review owner.
- `tools/check-story-review-readiness.py` exits `1` for Story 27.3 on the default branch through the empty-changed-set fail-closed path, not through any File List defect. Re-verified 2026-07-30 by code review: the same gate exits `0` with `Story review readiness validation passed.` when given the real reviewed changed set via `--changed-files-file` (8 in-scope paths, and 11 paths including the declared-excluded `references/` gitlinks). Owner: the concurrent `spec-resolve-story-gate-commit-path` session. Re-open trigger: when that spec lands, confirm the bare `--story-key` invocation is no longer a vacuous or misleading signal for a governed story on `main`.
- The C6 evidence-row gate can be satisfied by deleting rows rather than proving them. The 2026-07-30 correction moved the C1 umbrella row from `pending` to `complete` and deleted twelve `pending` child-gate rows, clearing thirteen mechanical C6 blockers by record edit alone; the gate now sees one evidence table where the story carried two. Not a defect of this story — the transfer is Administrator-approved and `epics.md` now carries a stronger 25-row table with a consequence-and-reopen-trigger column the deleted table lacked, so the record was moved, not erased. Owner: story-gate tooling. Re-open trigger: when `check-story-review-readiness.py` is next revised, make a `complete` completion state distinguishable from an administrative scope transfer that proved nothing.

## Deferred from: code review of 27-3-production-adapter-and-deployment-profile (2026-07-31)

Five 2026-07-31 `[Review][Decision]` items resolved by the Administrator that a code-review workflow has no authority to execute. Story creation, story registration, acceptance-criterion amendment, and proposal re-approval are `correct-course` routes; `story-scope-guard.md:38-40` binds the splitting route for every story it creates.

  - ID: 27.3-CR31
  - Status: open
  - Evidence: Corrected 2026-08-01 by approved Sprint Change Proposal 2026-08-01: the invalid Story 27.5/27.6 registrations and their checkpoint tables were withdrawn from `epics.md` and `sprint-status.yaml`. All twenty-five C1 definitions are held only in the proposal annex, with no current story owner or completion authority. This removes the non-compliant registrations but does not discharge the entry.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-31)
  - Target artifact: future separately tracked Story 27.5 and Story 27.6 files, plus their later-approved `epics.md` and `sprint-status.yaml` registrations
  - Re-open trigger: before either candidate Story 27.5 or Story 27.6 is registered or set `ready-for-dev`, and before any C1 gate is cited as evidence; discharged only when both successor files exist and every registered gate row carries its owner, a real evidence command or artifact, review state, and completion state.
  - Rationale: recorded 2026-07-31 by code review. Administrator decision: split the Story 27.5 gate set rather than de-registering it. Story 27.5 is registered at `sprint-status.yaml:459` and in `epics.md` while this diff removed its `Historical Context Classification`, `Slice Proof`, and 25-row checkpoint table to `sprint-change-proposal-2026-07-30.md` Annex A marked "held, **not registered**", and no `27-5-*` story file exists - so neither branch of `epics.md:555` is satisfied on any surface. `story-scope-guard.md:27-31` binds at registration at any status, including `backlog`. Combined with decision `27.3-CR34`, the arithmetic is: 11 evidence-bearing rows register now (C1.15-C1.24 plus C1.25), and 14 activation-blocked gates (C1.1-C1.12, C1.14, and C1.13) spin out into a newly numbered story. Owner: correct-course route plus the Administrator. Consequence: while unregistered, no C1 gate has an accountable owner or evidence definition on any binding surface. **Decision superseded 2026-08-01:** the Administrator selected withdrawal to a held proposal annex, not a policy amendment and not producer authoring in this phase. An accepted activation blocker remains insufficient under the unchanged guard.

  - ID: 27.3-CR32
  - Status: resolved
  - Evidence: Resolved 2026-07-31 by approved Sprint Change Proposal 2026-07-31, edit E2: both governed AC6 copies now state the dynamic-enumeration contract, list evidence production and upload as failing steps, carry the anti-skip clause, and no longer make substitution mandatory — zero vault-typed Components is stated as a passing observation so the lane stays able to discharge CR29/CR30.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-31)
  - Target artifact: _bmad-output/planning-artifacts/epics.md AC6 and the Story 27.3 artifact AC6
  - Re-open trigger: before checkpoint C2 is cited as passed against the amended lane; discharged when both governed AC6 copies describe the dynamic-enumeration contract in identical terms and a fail-closed enumeration guard exists in code.
  - Rationale: recorded 2026-07-31 by code review. Administrator decision: amend AC6 to the dynamic contract rather than restoring the named-two code. AC6 pins "**the two**" vault-typed Components while `tools/verify-production-deployment.ps1:918-926` deleted the zero-component throw and `production_deployment_evidence_test.py:855` actively forbids the named-two shape, so the shipped code contradicts the ratified AC on `required substitution` and `observed post-patch types`. The amendment must also cure the separate defect that the two governed copies are not the same contract: the story copy at `:55` carries the anti-skip clause and the evidence-upload step, `epics.md:4916` carries neither, and both drop "evidence production" from the approved fail list. Owner: correct-course route. Consequence: until amended, a green AC6 lane proves a contract the acceptance criterion does not state. **Dated correction 2026-08-01 by code review (second pass), owed since 2026-07-31.** The approved proposal's section 5 asserts "the source `deferred-work.md` rationales carry a dated correction note"; this rationale carried none. Re-derived: `tools/verify-production-deployment.ps1:918-926` pointed at unrelated Deployment rollout code (the referenced throw was at `:863-865` pre-patch), and `production_deployment_evidence_test.py:855` is an ordering assert, not the named-shape prohibition at `:862`. Both underlying defects were real; only the anchors were wrong.

  - ID: 27.3-CR33
  - Status: resolved
  - Evidence: Resolved 2026-07-31 by approved Sprint Change Proposal 2026-07-31, edit E5: four dated correction notes applied at `:102-104`, `:115`, `:305`, and success criteria 4 and 5; Annex A marked superseded and provenance-only; dated second approval recorded.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-31)
  - Target artifact: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-30.md
  - Re-open trigger: before any artifact edit is justified by citing this proposal's authorization; discharged when a dated second approval records the post-approval content.
  - Rationale: recorded 2026-07-31 by code review. Administrator decision: re-approve the amended proposal. Approximately 55 lines of normative content (Annex A) and a rewritten `:118` authorization row were added after approval, while `:351` authorizes "**only** the artifact edits enumerated here" and `:404` records approval of "**the complete proposal**", undated for the amendment. The re-approval must also cure the four statements this diff falsified and left standing - `:115`, `:102-104`, `:305`, and success criteria 4 and 5 - which still assert that `epics.md` gains the Story 27.5 guard records and checkpoint table that the same diff removed. Owner: the Administrator. Consequence: the recorded approval does not cover what the document now contains. **Dated correction 2026-08-01 by code review (second pass), owed since 2026-07-31.** Re-derived against the current file: "approximately 55 lines" of Annex A is **59** (`sprint-change-proposal-2026-07-30.md:434-492`), and the quoted "complete proposal" approval wording cited as `:404` is at `:423`. The substance - that normative content was appended post-approval - holds.

  - ID: 27.3-CR34
  - Status: open
  - Evidence: Prior resolution superseded 2026-08-01 by approved Sprint Change Proposal 2026-08-01. C1.13's configured-capacity admission remains bound to the exact running target in held Story 27.6 AC13; the unit lane is only a precondition. Because Story 27.6 is withdrawn from registration, no registered criterion currently owns C1.13.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-31)
  - Target artifact: future Story 27.6 file and its later-approved registration; held draft authority: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01.md` Annex B
  - Re-open trigger: before C1.13 is cited as passed or a candidate Story 27.6 is registered; discharged when C1.13 has a registered owning story file whose criterion requires the running-target observation and treats the unit lane only as a precondition.
  - Rationale: recorded 2026-07-31 by code review. Administrator decision: C1.13 keeps its running-target binding and the passing unit lane is a precondition, not discharge. `epics.md:4984-4986` and AC2 at `:4912` bind configured-capacity admission to "**the exact running target**", and `story-scope-guard.md:68-69` forbids closing on internal-only proof where observable evidence is required; the 2026-07-30 finding at `:497` ruled the opposite, that C1.13 "needs no activation gate" because its evidence is a local unittest assertion. That ruling is withdrawn. C1.13 therefore joins the activation-blocked set, changing `27.3-CR31`'s split from 12/13 to 11/14. Owner: correct-course route. Consequence: C1.13 cannot be closed from the repository alone. **Current consequence 2026-08-01:** the binding criterion is preserved but deliberately unregistered, so C1.13 remains unowned and cannot be completed.

  - ID: 27.3-CR35
  - Status: resolved
  - Evidence: Resolved 2026-07-31 by approved Sprint Change Proposal 2026-07-31, edit E1: the Story 27.3 title and `## Story` statement in both copies describe the retained C0/C2-C4 scope, and a dated supersede note is present inside the `epics.md` Story 27.3 block.
  - Source story: 27-3-production-adapter-and-deployment-profile (code review 2026-07-31)
  - Target artifact: the Story 27.3 artifact title and `## Story` statement, and the `epics.md` Story 27.3 block
  - Re-open trigger: before Story 27.3 is set `done`; discharged when the title, `## Story` statement, and `epics.md:4903-4907` describe the retained C0/C2-C4 scope with a dated supersede note.
  - Rationale: recorded 2026-07-31 by code review. Administrator decision: amend the title and statement to the retained scope rather than leaving the deferral standing. `:13-14` and `:27-29` still read "qualified against every C1 gate" and `epics.md:4903-4907` is identical and carries no supersede note inside the Story 27.3 block, so after the correction that removed all C1 ownership the artifacts assert both readings simultaneously. The preceding review knowingly deferred this at `:494`. Owner: correct-course route. Consequence: the first thing a reader meets is a scope claim the story no longer holds. **Dated correction 2026-08-01 by code review (second pass), owed since 2026-07-31.** Re-derived: the story's `:13-14` named the title, not the quoted phrase, which was at `:28`. The substance - that the title and `## Story` statement still described transferred scope - holds.

  - ID: 27.3-CR36
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (correct-course 2026-07-31)
  - Target artifact: tools/verify-production-deployment.ps1, Wait-AggregateStatus (:789-800)
  - Re-open trigger: on the first production-deployment-verification lane failure whose terminal message is the `$TimeoutSeconds`-second startup-limit throw and whose reported uncredited port-forward capture is a material fraction of the budget; discharged when either the budget is re-sized against observed cold-start data or a bounded capture credit is restored with its ceiling stated in AC6.
  - Rationale: recorded 2026-07-31 by correct-course under the Administrator decision of the same date. The 2026-07-31 fix caps the effective startup total at `$TimeoutSeconds` and credits no runner-side port-forward capture, curing a prior form that bounded the CREDIT rather than the RESULT so the effective ceiling reached `2 x $TimeoutSeconds` - a container Kubernetes recorded Ready 119s after start passed the "60-second startup limit". The trade-off is disclosed in-code at `:795-797`: a stage whose port-forward capture is genuinely large can now fail a container that became ready inside its own budget. No such false red has been observed; this entry exists so the first one is read as a known trade-off rather than a new defect. Owner: the production-deployment-verification lane owner. Consequence: a cold-start CI environment can red the lane on capture overhead the contract does not charge to the container.

  - ID: 27.3-CR37
  - Status: open
  - Evidence: Re-derived 2026-08-01 at reachable HEAD `1d9e9c89ef53d877b4ec09face575c36e5889854`: `git merge-base --is-ancestor b391731c HEAD` exits `1`, so the prior discharge commit is not reachable. `git ls-tree HEAD` records gitlinks Builds `b529b665a6f076d07d218266ab74ca211f34f5a7`, EventStore `3ca3cbbf042365f5d876a3fe3d6cc19edd678e3b`, and Tenants `2cd7edf5088c54bbeced37d2f8164c36889b7cac`; `git submodule status` reports Builds at `+9bdb368d3da867f68a37f4ad7e1d696072543e03` while EventStore and Tenants match. The Builds blocker is live again and remains owned by the dependency session.
  - Source story: 27-3-production-adapter-and-deployment-profile (correct-course 2026-07-31)
  - Target artifact: references/Hexalith.Builds, references/Hexalith.EventStore, references/Hexalith.Tenants gitlinks
  - Re-open trigger: before any Story 27.3 commit is created; discharged when the owning session confirms the bumps were deliberate and commits them separately, or confirms they are inherited drift and `git submodule update -- <paths>` restores them.
  - Rationale: recorded 2026-07-31 by correct-course. Three `references/` gitlinks are dirty in the working tree and are excluded from the 2026-07-31 correction's File Scope: `Hexalith.Builds` 61e43b18 -> e85a319e, `Hexalith.EventStore` a40ab8a6 -> e4618d91, `Hexalith.Tenants` 33abe276 -> 625061bd (observed at correction time; superseded before commit - see Resolution). They must not ride along in a Story 27.3 commit. They are deliberately not reverted here: a concurrent session's dependency bump is not this route's to discard, and `project-context.md:112` requires explicit intent plus separate submodule commits. Owner: the session that produced the bumps. Consequence: an unrelated dependency change could be attributed to Story 27.3. **Re-opened 2026-08-01:** the unreachable commit and live Builds divergence satisfy this entry's own reopen trigger. This correction neither absorbs nor reverts the dependency owner's work.

  - ID: 27.3-CR38
  - Status: resolved
  - Evidence: Resolved 2026-08-01 by approved Sprint Change Proposal 2026-08-01. Runner-derived extraction reports File Scope 61/61 unique and File List 61/61 unique, with `scope_only=0` and `list_only=0`. Against baseline `272c33bc5d30d71ac46f20e703b9d5456e75a093`, the 60 tracked declarations produce `39 A / 21 M`; the explicitly untracked approved proposal adds one A, giving combined `matched 61/61`, `40 A / 21 M`. All eight concurrent-session and three reference-gitlink exclusions remain present.
  - Source story: 27-3-production-adapter-and-deployment-profile (created by correct-course 2026-07-31; creation and resolution ratified 2026-08-01 by approved Sprint Change Proposal 2026-08-01)
  - Target artifact: _bmad-output/implementation-artifacts/27-3-production-adapter-and-deployment-profile.md `### File List`
  - Re-open trigger: before Story 27.3 is set `review` or `done`, and at the next File List reconciliation; discharged when both omitted paths are listed and the declared cumulative count equals the actual bullet count.
  - Rationale: recorded 2026-07-31 by correct-course, surfaced by running `tools/check-story-review-readiness.py --derive-cumulative` by hand before committing the 2026-07-31 correction. Two defects, one new and one pre-existing. **New:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md` and `_bmad-output/process-notes/story-creation-lessons.md` were changed by the 2026-07-31 correction but are absent from the `### File List`; the gate flagged both correctly. They were committed under `Scope-Override:` trailers because they sit outside the declared File Scope, which is the sanctioned exception mechanism, but a Scope-Override authorizes the path - it does not add it to the File List. **Pre-existing:** the File List's own note declares "37 paths" while the section carries 58 bullets, so the declared cumulative count has been stale since some review after 2026-07-26 chunk 3b. Neither is repaired here: `story-phase-ledger.md` assigns cumulative recalculation to the dev-story and code-review routes, and the approved 2026-07-31 proposal authorized no File List or ledger edit. The same commit carries a `Story-Review-Readiness-Bypass:` trailer disclosing that 2 of its 316 C1 rows are these real omissions and 314 are unrelated in-flight work swept in by `--derive-cumulative`. Owner: the next Story 27.3 dev-story or code-review File List reconciliation. Consequence: the story's File List understates what the story changed, and its declared count cannot be trusted as arithmetic. **Ratified and resolved 2026-08-01:** the approving proposal is now declared in both governed 61-path sets; this closes the authorization gap and the count/list mismatch without absorbing excluded work.

  - ID: 27.3-CR39
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (created by correct-course 2026-07-31; creation ratified 2026-08-01 by approved Sprint Change Proposal 2026-08-01)
  - Target artifact: .githooks/pre-commit and tools/check-story-file-scope.py, tracked by spec-resolve-story-gate-commit-path
  - Re-open trigger: the next time a governed commit needs a `Scope-Override:` trailer; discharged when a story-keyed branch can authorize an out-of-scope path through the documented trailer without renaming the branch.
  - Rationale: recorded 2026-07-31 by correct-course while committing the 2026-07-31 correction. **The `Scope-Override:` mechanism is structurally unusable on a story-keyed branch.** `.githooks/pre-commit` invokes `check-story-file-scope.py` with `--defer-unresolved-owner` but **without** `--commit-message-file`, because pre-commit cannot see the message. When the branch name contains a full story key the owner resolves from the branch, `--defer-unresolved-owner` therefore does not engage, and the full File Scope check runs with every `Scope-Override:` trailer invisible - so a legitimately authorized path fails at pre-commit and never reaches the commit-msg gate that would accept it. Reproduced exactly: identical staged set and message, `--branch-name fix/27-3-production-adapter-and-deployment-profile` exits 1, `--branch-name fix/c1-gate-split-and-ac6-alignment-2026-07-31` exits 0 with "deferring story-scope validation to commit-msg". The only non-bypass workaround is to name the branch so it carries no story key and let the `Story-Key:` trailer carry attribution instead, which is what this commit does - it inverts the usual convention of naming a branch after its story. This is the same class as the deadlock `spec-resolve-story-gate-commit-path` already fixed for standalone specs, on the other hook. Owner: `spec-resolve-story-gate-commit-path`. Consequence: either the branch cannot follow the story-naming convention, or an approved out-of-scope path cannot be committed without `--no-verify`, which the shared AI baseline forbids. **Ratified 2026-08-01:** this proposal supplies the authorization missing from the 2026-07-31 proposal; status remains `open` under `spec-resolve-story-gate-commit-path` ownership.

  - ID: 27.3-CR40
  - Status: open
  - Source story: 27-3-production-adapter-and-deployment-profile (correct-course 2026-08-01)
  - Target artifact: future separately tracked Story 27.5 and Story 27.6 files
  - Re-open trigger: before either candidate story is registered or set `ready-for-dev`; discharged when every held C1 gate's work appears in exactly one owning story file, every task maps to its gate, and every checkpoint row has a real evidence command or artifact.
  - Rationale: Task 1 was removed, not completed, from Story 27.3's active task set by approved Sprint Change Proposal 2026-08-01. Its C1.1-C1.25 work remains held in that proposal's annex and cannot be selected, cited, or completed until compliant successor story files own it. Owner: Product Owner plus Hexalith Platform Operations. Consequence: no held C1 work has current execution or completion authority.
- source_spec: `_bmad-output/implementation-artifacts/spec-gh-30655137033-fix-ci-cd-issues.md`
  summary: Reconcile the documented approximately five-minute `integration-fast` budget with observed 15–20 minute executions.
  evidence: The final exact selector passed in 15m38s and earlier unchanged broad runs took 19–20 minutes, while `tests/README.md` still defines this lane as an approximately five-minute budget; the discrepancy predates and is not caused by the OpenBao/MCP stabilization patch.

- source_spec: `_bmad-output/implementation-artifacts/spec-27-2-lifecycle-checkpoint-gaps-cr42-cr46.md`
  summary: Make the normal story-readiness gate require an executed C0 receipt and reviewer-owned command evidence before accepting review or done.
  evidence: The current readiness gate can accept a story whose C0 remains blocked because it validates declared paths, status vocabulary, sprint-status agreement, and evidence-table row state, but does not mechanically require this receipt row or prove that its recorded commands executed; independent review remains the fail-closed control.

- source_spec: none
  summary: Fix integration-fast Dapr actor Connection refused failures from CI run 30990821240 (rate limiting + tenant configuration tests against 127.0.0.1:35131).
  evidence: Split from the CI fix intent so BMAD customization fixture drift can ship independently without waiting on sidecar/harness diagnosis.


- source_spec: `_bmad-output/implementation-artifacts/spec-gh-30990821240-bmad-customization-fixtures.md`
  summary: Refresh historical governance docs that still cite `_bmad/custom/bmad-generate-project-context.toml` after the skill rename.
  evidence: Cross-tenant carry-forward and related process notes still document the deleted generate custom path and generate-skill verification commands; this fixture fix deleted that path without updating those historical references.


- source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md`
  summary: Historical container rebuilds are not bit-identical, so re-running Recover Partial Release after a successful image push hits immutable digest conflicts with no release-only workflow path.
  evidence: 2.6.5 first push succeeded then evidence failed; second run conflicted on config digest. 2.6.0-2.6.4 Releases had to be completed offline from already-present remote tags.
- source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md`
  summary: Workflow 2-or-4 Server/MCP evidence gates are only source-text pinned in CiTestInventoryTests, not executed as PowerShell.
  evidence: Verification-gap review showed deleting the hasServer/hasMcp throw while leaving pinned substrings would keep CiTestInventoryTests green.
- source_spec: `_bmad-output/implementation-artifacts/spec-gh-29354084222-fix-ci-cd-issues.md`
  summary: PARTIAL PUBLISH incidents for 2.6.0-2.6.7 were already closed before evidence-backed recovery completed.
  evidence: Issues #22-#33 show closedAt before the 2026-08-08 recovery; complete-partial-release only closes open issues.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md`
  summary: Align Story 24.6 Cross-Tenant Negative Evidence with the three axis-specific search classes required before removing the all-axis test.
  evidence: Review found the registered 24.6 evidence contract omits GraphScopedSearchIntegrationTests, SyntacticSearchIntegrationTests, and SemanticSearchIntegrationTests that Dev Notes and Planned Verification require citing.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md`
  summary: Resolve Story 24.7 AC1 wording so missing FT.INFO dimensions fail closed instead of ambiguous “all available” vs “all three” agreement.
  evidence: The approved proposal says “all available values agree” while the registered story and epics.md require “all three values agree,” leaving undefined behavior when one index dimension is missing.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md`
  summary: Define blank or whitespace-only tenantId handling for Story 24.9 marker diagnostics.
  evidence: Edge-case review showed proven-active hashes with empty/whitespace tenantId are unclassified and can be mislabeled as foreign contamination.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md`
  summary: Add a retrospective addendum or reopen note when epic-24 returns to in-progress after epic-24-retrospective is done for Stories 24.6-24.9.
  evidence: sprint-status.yaml reopens epic-24 while leaving epic-24-retrospective done with no addendum covering the residual backlog registration.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md`
  summary: Align Story 24.6 accepted-blocker schema so “proof boundary” is required consistently across proposal and registered story/epics text.
  evidence: Registered AC4 requires a proof boundary field while the matching proposal AC omits it.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-24-verifier-residual-backlog-2026-08-04.md`
  summary: Name the concrete RedisEmbeddingMigrationStoreTests method required by Story 24.8 Cross-Tenant Negative Evidence.
  evidence: Planned Verification commands the migration store tests without a named assertion contract in the evidence table.

- source_spec: `_bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md`
  summary: Add a reciprocal pointer from docs/operations/route-surface.md to the new directory-ingestion authoritative contract.
  evidence: Directory guidance links to route-surface as the prior route home, but route-surface is outside this story File Scope and gained no back-link.
- source_spec: `_bmad-output/implementation-artifacts/spec-epic-23-documentation-verification.md`
  summary: Reconcile AccessTelemetry C1 ownership guard text with Story 27.21 C1.15 registration and the frozen Never/test File Scope carve-out.
  evidence: This story co-shipped AccessTelemetryRetentionDecisionTests pinning unowned C1 text; later 27.21 registration and planning copies partially supersede that pin without updating the guard.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md`
  summary: OpenBao root/unseal/scoped tokens and KV field values still ride kubectl exec / bao argv during disposable bootstrap.
  evidence: Review found BAO_TOKEN= and key=value on process argv; Protect-EvidenceText redacts evidence output but cannot hide argv from node process lists or kubectl audit trails without redesigning away from bao CLI over kubectl exec.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md`
  summary: Disposable OpenBao namespace objects and unseal material are not retired by OpenBao helper code beyond local work-dir deletion.
  evidence: Review noted only the local TLS/work directory is deleted; cluster-scoped OpenBao Deployment/Secrets/ConfigMaps and unseal keys rely on kind cluster teardown rather than an explicit OpenBao cleanup stage.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md`
  summary: Protect-EvidenceText token regex can redact ordinary s./b./r. substrings in diagnostics.
  evidence: Blind-hunter noted `\b(?:hvs|hvb|hvr|s|b|r)\.[A-Za-z0-9_-]{16,}\b` is broader than OpenBao/Vault token shapes and can over-redact unrelated diagnostics.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md`
  summary: Disposable OpenBao container still runs with readOnlyRootFilesystem false despite emptyDir data volume.
  evidence: Edge/hardening review; securityContext is otherwise hardened but root filesystem writability remains a residual disposable-verifier hardening gap.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-29804293613-fix-production-deployment-verification.md`
  summary: Full stubbed-kubectl execution suite for OpenBao init/unseal/KV/policy/seed/token paths is not unit-tested beyond source pins and kind e2e.
  evidence: Verification-gap review; kind verification exercised the live path, but Confirm/Get-HealthResponse-style stub execution coverage was not added for every OpenBao helper function.

## Infrastructure-Dependency Abstraction (IDA) Deferred (2026-08-09)

- source_spec: `_bmad-output/implementation-artifacts/spec-infrastructure-dependency-abstraction.md`
  summary: Remove unreferenced `RedisPlaceholder` port-constant compat surface on the next owned breaking major once no external consumer depends on it (F9).

  - ID: IDA-F9-REDISPLACEHOLDER-REMOVAL
  - Status: open
  - Source story: spec-infrastructure-dependency-abstraction
  - Target artifact: src/Hexalith.Memories.Redis/RedisPlaceholder.cs
  - Re-open trigger: an owned breaking major of the Redis package is cut, or an external consumer audit confirms zero remaining references to `DefaultRedisPort` / `DefaultFalkorDbPort`.
  - Rationale: Constants are compile-time compat only (open no connections); removal is deferred to avoid an unforced package break while F9 already labels them non-leak.

## Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-12)

Nine low-severity items remain carried forward from the Story 24.6 adversarial code review; two more
items from that review are resolved below. None blocks the story's content-isolation proof, which was
independently re-executed and passed.

- **Runbook proof text is duplicated across two operator documents.**

  - ID: 24.6-CR-W1
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: docs/operations/route-surface.md
  - Rationale: Roughly 30 lines of build, port-discovery, and proof-invocation text remain duplicated here and in companion `docs/operations/tenant-onboarding-offboarding.md`. `OperationalRunbookSetTests.GraphIsolationEvidenceBoundary_SeparatesStructuralAndContentProof` asserts that each section contains the required tokens; it does not compare the sections to each other, and the sections are not identical. Extracting shared documentation is outside the bounded proof closure.
  - Re-open trigger: Either operator document drops a required token, a later change treats the two sections as byte-identical, or the documentation build gains a supported shared-snippet mechanism.

- **Collision fixtures remain in the shared real-backend topology.**

  - ID: 24.6-CR-W2
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: The proof seeds collision nodes, edges, and two provisioned tenants without teardown; unique tenant identifiers prevent cross-test identity conflicts, while shared-topology cleanup is a wider fixture-lifecycle concern.
  - Re-open trigger: A later test observes these records, integration storage growth becomes material, or the shared fixture adds a safe tenant teardown API.

- **The collision proof assumes newly provisioned graphs contain no relationships.**

  - ID: 24.6-CR-W3
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: A pre-existing relationship would report a failed collision precondition rather than a dedicated fixture-precondition assertion; unique tenant provisioning makes that condition unlikely and diagnostic refinement is outside the closure.
  - Re-open trigger: The collision precondition fails on a non-empty newly provisioned graph or fixture provisioning begins seeding relationships.

- **Graph proof coverage is one bounded edge/origin/source/depth shape.**

  - ID: 24.6-CR-W4
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: Story 24.6 proves the required `EdgeType.CausedBy`, `EdgeOrigin.Explicit`, `SourceType.File`, depth-one collision fixture; additional relationship variants and traversal depths are useful expansion coverage but not part of the accepted NFR8 slice.
  - Re-open trigger: A new edge type, origin, source type, or deeper traversal path changes tenant-routing behavior or a leakage defect appears outside the proven fixture.

- **The verifier class-level contract had been narrowed beyond Story 24.6.**

  - ID: 24.6-CR-W5
  - Status: resolved
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
  - Evidence: Fifth-pass Decision D2 restored the broad class-level architectural-isolation XML summary and kept the structural-only hedge local to `CheckGraphIsolationAsync`; focused verifier tests passed after the repair.
  - Re-open trigger: A future change again applies the graph-specific structural-only limitation to the verifier's class-wide Redis and semantic responsibilities.

- **Non-passing graph-isolation branches do not repeat the structural-only label.**

  - ID: 24.6-CR-W6
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
  - Rationale: The method-level contract and successful HTTP-visible detail establish the proof boundary; duplicating the wording across failure and unavailable branches is a diagnostic-consistency improvement outside this bounded repair.
  - Re-open trigger: An operator or automated consumer interprets a failed or unavailable `GraphIsolation` result as graph-content proof, or those branch messages are otherwise revised.

- **The HTTP-visible graph detail has no explicit length or format contract.**

  - ID: 24.6-CR-W7
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
  - Rationale: The roughly 330-character prose detail is intentionally operator-facing and is now pinned for its required structural-only, `GRAPH.LIST`, and proof-method tokens; introducing a new structured or maximum-length contract would expand the public API surface.
  - Re-open trigger: The V1 response receives a formal details-length/format requirement or an operator surface truncates the required proof citation.

- **Production graph write-path tenant selection lacks a direct negative control.**

  - ID: 24.6-CR-W8
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestionPipelineTests.cs
  - Rationale: The collision proof writes directly through `falkor.SelectGraph(tenantId)` and proves authenticated read-path routing and content locality; production `IndexGraphActivity` tenant scoping is only pinned indirectly by post-ingestion node counts and requires a distinct ingestion-owned negative scenario.
  - Re-open trigger: An ingestion story changes graph selection or claims direct write-path cross-tenant proof, or a tenant A ingestion is observed in tenant B's graph.

- **The story embeds the Epic 23 checklist-preservation shell loop.**

  - ID: 24.6-CR-W9
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: _bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md
  - Rationale: The inline loop is executable and preserves the exact evidence used at review time; replacing it with only a document citation would reduce local reproducibility, while deduplicating governance commands is outside the proof closure.
  - Re-open trigger: The embedded command diverges from `spec-keep-epic-23-ingestion-invariants-on-epic-24-and-epic-25-review-checklists.md` or becomes non-rerunnable.

- **Some traversal response assertions do not prove JSON wire presence.**

  - ID: 24.6-CR-W10
  - Status: carried-forward
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: `Degraded`, `OmittedCount`, `UnavailableAxes`, and `PrimaryPathIntact` are asserted after deserialization, so omitted default-valued members could pass; the required node, edge, marker, completeness, and topology assertions still fail closed for the accepted content-isolation fixture.
  - Re-open trigger: The API serializer or response contract changes default-member emission, or these fields become part of the content-isolation acceptance claim.

- **Verifier unit mocks no longer rely on unconfigured graph-query defaults.**

  - ID: 24.6-CR-W11
  - Status: resolved
  - Source story: spec-24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs
  - Evidence: Fifth-pass repair inspects all `ReceivedCalls()` arguments for every `GRAPH.*` command and requires the non-empty executed set to contain only `GRAPH.LIST`; the companion source guard scans every `TenantIsolationVerifier*.cs` file and rejects any other graph command token.
  - Re-open trigger: A verifier collaborator can execute a graph command without being captured by `ReceivedCalls()`, or graph-command construction moves outside the guarded source family.

## Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence (2026-08-13)

Seven items deferred during the Story 24.6 fifth-pass adversarial code review. None threatens AC1,
which was independently confirmed to hold against a real FalkorDB backend. Each carries the structured
field block required by the schema above; the eleven first-pass entries recorded on 2026-08-12 now do
as well.

- **Verifier source guard still lives in a runbook test class.**

  - ID: 24.6-F5-W1
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.Server.Tests/Deployment/OperationalRunbookSetTests.cs
  - Rationale: The first-pass patch read "strengthen **and relocate** the source-text guard". N5 strengthened it on 2026-08-13, but the assertion about `TenantIsolationVerifier.cs` source still sits in the runbook/deployment doc-contract class, so a verifier regression is reported by a test named for runbooks. Relocation is cosmetic to behaviour and was not attempted while the guard is green.
  - Re-open trigger: The guard fails and the failure is misattributed to runbook content, or another verifier source assertion is added to the same class.

- **`ReconnectPrimaryDaprClients` disposes before installing replacements.**

  - ID: 24.6-F5-W2
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
  - Rationale: The rewritten method constructs both actor-proxy replacements first and only then disposes `_actorHttpMessageHandler` and swaps `_actorProxyFactory`/`_actorProxyOptions`. The original dispose-before-assign window is closed; a brief null window remains during the swap. Not currently reachable: `[Collection("AspireIngestionPipeline")]` serialises the tests and the restart regression creates its proxy after the rotation. The untested allocation-failure cleanup path is recorded separately as `24.6-F8-W2`.
  - Re-open trigger: Any test caches an actor proxy across the OpenBao restart, or the collection gains parallel execution.

- **Reconnect fires only when the sidecar endpoint changes.**

  - ID: 24.6-F5-W3
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
  - Rationale: The guard is `if (currentDaprEndpoint != DaprSidecarHttpEndpoint)`, so a sidecar that restarts on the same port skips the reconnect entirely and the fixture keeps pooled connections to the killed process. The correct trigger condition (endpoint change **or** process restart) needs a restart signal the fixture does not currently expose.
  - Re-open trigger: The restart regression flakes with a connection error on an unchanged port, or a process-restart signal becomes available.

- **Traversal assertions dereference without null guards.**

  - ID: 24.6-F5-W4
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: `AssertTraversalIsFixtureLocal` dereferences `Nodes`, per-node `Edges`, and `GapMarkers` without null checks, so a response that omits or nulls one of them raises a `NullReferenceException` instead of an assertion naming the field that lost its marker. Diagnostic quality only; the assertions still fail closed.
  - Re-open trigger: A traversal failure is reported as a `NullReferenceException`, or the traversal contract makes any of those members nullable.

- **No cancellation coverage on `VerifyAsync`.**

  - ID: 24.6-F5-W5
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs
  - Rationale: No test invokes `VerifyAsync` with an already-cancelled token, so the graph check's cancellation behaviour is unpinned and a regression would pass unnoticed. Outside AC3's structural-only scope.
  - Re-open trigger: Cancellation handling in `TenantIsolationVerifier` is changed, or a cancellation defect is observed in the verify endpoint.

- **No `/traverse` denial rows for invalid or out-of-range `depth`.**

  - ID: 24.6-F5-W6
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.Server.Tests/Authentication/ServerEndpointAuthorizationTests.cs
  - Rationale: The Story 20.2 denial-before-dependency rows cover missing and blank `startNodeId` but not a malformed or out-of-range `depth`, where a 400 could pre-empt the 403 and reveal that the handler was reached. Beyond AC1's stated boundary.
  - Re-open trigger: `depth` validation moves relative to the tenant authorization filter, or a new query parameter is added to the traverse route.

- **Restart regression dereferences an actor config without a null guard.**

  - ID: 24.6-F5-W7
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/OpenBaoTopologyIntegrationTests.cs
  - Rationale: The post-rotation actor call dereferences its result without a null guard and silently depends on the `OpenBaoRecoveryTenantId` tenant carrying a seeded embedding configuration, so a fixture-data gap surfaces as a `NullReferenceException` rather than a named assertion.
  - Re-open trigger: The regression fails with a `NullReferenceException`, or `OpenBaoRecoveryTenantId` seeding changes.

## Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — sixth pass (2026-08-13)

Five low-severity items are carried from the Story 24.6 sixth-pass closure review. None threatens AC1,
which was independently re-confirmed to hold against a real FalkorDB backend this pass. Each carries the
structured field block required by the schema above.

- **The HTTP-visible AC3 citation is hard-coded where the unit guards are manifest-bound.**

  - ID: 24.6-F6-W1
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: F5-P8 bound the operator proof citation to `tools/integration-fast-required-surfaces.txt` through `OperationalRunbookSetTests.GraphContentProofCitation`, and both Server.Tests guards now derive it. The real-backend assertion cannot reach that `internal` member from a different assembly, so it keeps the literal `TenantIsolationIntegrationTests.VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes`. The binding is fail-closed — a manifest-driven rename reds the unit lane — so this is maintenance duplication in three places, not an escape hatch, and the fix would mean duplicating the manifest reader into the integration assembly.
  - Re-open trigger: The graph-content proof method is renamed or re-keyed in the manifest, or a shared test-support assembly becomes available to both test projects.

- **The restored verifier class-level summary has no test pinning either wording.**

  - ID: 24.6-F6-W2
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
  - Rationale: `24.6-CR-W5` was closed on the strength of fifth-pass Decision F5-D2 restoring the broad architectural-isolation XML summary and keeping the structural-only hedge local to `CheckGraphIsolationAsync`. The original finding's second half — "no test pins either wording" — was not addressed, so the entry's own re-open trigger ("a future change again applies the graph-specific structural-only limitation to the verifier's class-wide responsibilities") has no detector and would have to be caught by review. The reverted wording is net-zero against baseline `0ecdffed`, so nothing regressed; only the guard is missing.
  - Re-open trigger: The class-level summary is edited again in either direction, or a Story 24.7-24.9 slice re-scopes the verifier's Redis marker responsibilities.

- **The graph-check lookup throws an undiagnosable exception when the check is absent.**

  - ID: 24.6-F6-W3
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: The new HTTP-observable AC3 block resolves the check with `result.Checks.Single(check => check.CheckName == "GraphIsolation")`. If the verifier stops emitting `GraphIsolation`, or emits it twice, the test fails with a bare `InvalidOperationException` naming neither the check nor the contract, instead of a Shouldly assertion. The surrounding `AssertCoreIsolationChecksPassed(result)` already fails closed on a missing check, so the diagnosis cost is the only impact.
  - Re-open trigger: The test fails with `InvalidOperationException`, or the verifier begins emitting per-backend `GraphIsolation` results.

- **Node-marker assertions lack a mutation-sensitivity control.**

  - ID: 24.6-F6-W4
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: The positive real-backend fixture proves tenant-local node and edge markers, while the planted-marker mutation control exercises only the edge-marker assertion. The Administrator ratified C1's completed boundary as edge-only for mutation sensitivity; a node-marker control remains useful hardening but is outside that boundary.
  - Re-open trigger: A node-marker assertion is weakened or removed, a foreign-node regression appears, or Story 24.6's ratified C1 mutation-sensitivity boundary is reopened.

- **Graph-isolation verification does not convert a FalkorDB `RedisTimeoutException` into a failed backend check.**

  - ID: 24.6-F6-W5
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
  - Rationale: `CheckGraphIsolationAsync` catches `RedisConnectionException` and `RedisServerException` but not `RedisTimeoutException`, so a timeout can escape the verifier instead of preserving the existing graceful backend-unavailable result shape.
  - Re-open trigger: A FalkorDB timeout escapes `VerifyAsync` as an unhandled exception, or a later story converts `RedisTimeoutException` into a failed `GraphIsolation` check.

## Deferred from: bmad-build review of spec-pushall-sync-2026-08-09 (2026-08-13)

- source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md`
  summary: Hexalith.Builds still has an uncommitted Props/Directory.Packages.props change after the 2026-08-09 envelope closed.
  evidence: The working tree shows `references/Hexalith.Builds` dirty at `5d268c6b` with `Props/Directory.Packages.props` modified. That leftover is owned by `spec-submodule-bumps-2026-08-11.md`, not this envelope, which was required to preserve unrelated root work.

- source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md`
  summary: spec-pushall-sync-2026-08-05 remains ready-for-dev with overlapping Builds, EventStore, and FrontComposer File Scope.
  evidence: The 2026-08-05 envelope still has an unchecked superproject-push task and was not superseded or partitioned by the 2026-08-09 closeout, so a later operator can restage the same gitlinks under a second Story-Key.

- source_spec: `_bmad-output/implementation-artifacts/spec-pushall-sync-2026-08-09.md`
  summary: Direct origin/main push for authorized /pushall envelopes still trips GitHub branch-protection (PR required, expected status checks).
  evidence: Push `3e92ca36..8d47a46a` succeeded while GitHub reported a branch-protection bypass. This envelope's remaining task is to push the superproject, matching prior /pushall specs; the protection warning is a standing process tension, not a defect unique to this snapshot.

## Deferred from: bmad-build review of spec-submodule-bumps-2026-08-11 (2026-08-13)

- source_spec: `_bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md`
  summary: Builds catalog still pins HexalithMemoriesVersion at 2.20.7 while NuGet Hexalith.Memories.Contracts is 2.20.11.
  evidence: Memories consumes its own contracts via ProjectReference, so this pin is not a direct PackageReference AC failure; bumping it locally would move Builds off origin/main unless a Builds PR lands first.

- source_spec: `_bmad-output/implementation-artifacts/spec-submodule-bumps-2026-08-11.md`
  summary: Restore/build still surfaces NU1903 for SSH.NET 2025.1.0.
  evidence: Pre-existing advisory warning observed during the Release package-mode verification of this dependency refresh; unrelated to the submodule gitlink bumps.

## Deferred from: code review of spec-24-7-tenant-configured-vector-dimension-verification (2026-08-13)

- source_spec: `_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md`
  summary: TenantIsolationVerifier constructor leaves the four pre-existing parameters unguarded.
  evidence: Only the new `embeddingConfigProvider` parameter gained an ArgumentNullException guard; `registry`, `redis`, `falkorDb`, and `logger` predate Story 24.7 and still surface null as NullReferenceException at first use, deviating from the documented `ArgumentNullException.ThrowIfNull` boundary rule.

- source_spec: `_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md`
  summary: Make the concrete tenant embedding configuration provider stop its actor read when caller cancellation wins.
  evidence: `TenantEmbeddingConfigProvider.GetAsync` accepts a cancellation token but awaits `GetEmbeddingConfigAsync()` without applying it, so `TenantIsolationVerifier` can stop waiting through `WaitAsync` while the actor call continues and may populate the cache after the verification request is cancelled.

- source_spec: `_bmad-output/implementation-artifacts/spec-24-7-tenant-configured-vector-dimension-verification.md`
  summary: Bound semantic-isolation mismatch evidence returned for large tenant key sets.
  evidence: `ScanHashPrefixForTenantFieldMismatchesAsync` records every missing or foreign tenant marker and `CheckSemanticIsolationAsync` joins the full list into `Details`; this behavior predates Story 24.7 and can produce an unbounded diagnostic response when many hashes are contaminated.

## Deferred from: code review of spec-24-6-graph-content-level-tenant-isolation-evidence — eighth pass (2026-08-14)

Four items are carried from the Story 24.6 eighth-pass review. None threatens AC1, AC2, or AC3, each of
which was re-confirmed to hold in substance this pass. Each entry below carries the full structured field
block required by the schema above.

- **`GraphIsolation` discloses a cluster-wide graph-database count over a per-tenant endpoint.**

  - ID: 24.6-F8-W1
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
  - Rationale: The success `Details` string reports `({graphDatabases.Count} graph database(s))` over `POST /api/v1/tenants/{tenantId}/verify`, which is a count of other tenants returned to a single tenant's caller. The count predates Story 24.6 and the endpoint is operator-facing rather than tenant-facing, so this is not a live cross-tenant data leak. It is recorded because this range rewrote that exact string for a story whose thesis is not overstating isolation evidence, and no existing entry covers it.
  - Re-open trigger: The verify endpoint becomes reachable by a tenant-scoped caller, or any story re-scopes `GraphIsolation` evidence semantics.

- **The allocation-failure cleanup path in `ReconnectPrimaryDaprClients` has no test in any lane.**

  - ID: 24.6-F8-W2
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Fixtures/AspireIngestionPipelineFixture.cs
  - Rationale: The seventh pass added a `try`/`catch` that disposes the partially constructed `HttpClientHandler` and state client on construction failure and rethrows. The ledger records that phase as `+0 test cases / +0 test methods`, and the path is not reachable from any existing test because it requires a client construction failure mid-rotation. Forcing it would mean injecting a fault into fixture startup, which the current fixture design does not expose.
  - Re-open trigger: The fixture gains a fault-injection seam, or a rotation failure is observed in CI leaving a leaked handler.

- **The planted-marker negative control leaves its mutation in a provisioned tenant graph and is now a required CI surface.**

  - ID: 24.6-F8-W3
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: `VerifyTenant_PlantedForeignGraphEdgeMarker_CollisionAssertionsDetectLeakage` writes a foreign edge marker into tenant A's real graph with no teardown, and this range pinned it as a required `integration-fast` method surface. Each run provisions fresh GUID-suffixed tenants, so the corruption does not cross runs; the residual concern is that pinning a deliberately data-corrupting method into the required lane widens exactly the fixture-cleanup risk `24.6-CR-W2` already describes, without either entry recording the change.
  - Re-open trigger: A test outside this method observes a foreign marker on a shared fixture tenant, or the fixture moves to reused rather than per-run tenant identifiers.

- **The new assertions lack defensive guards that would name the failing field instead of throwing.**

  - ID: 24.6-F8-W4
  - Status: carried-forward
  - Source story: 24-6-graph-content-level-tenant-isolation-evidence
  - Target artifact: tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
  - Rationale: Six hardening gaps were identified across the new assertions: no null guards on `traversal.Nodes`, `traversal.GapMarkers`, or per-node `Edges`; no already-cancelled `CancellationToken` case for `VerifyAsync`; no empty or null `GRAPH.LIST` case pinning `ParseGraphList`; `Single()` rather than `ShouldHaveSingleItem` for the graph-check lookup; no linked cancellation token on the seed query, so a command abandoned by `WaitAsync` is not actually cancelled; and no null guard on the OpenBao recovery tenant's embedding configuration. Each degrades diagnosis quality on failure rather than weakening the proof — the assertions themselves fail closed — so they are hardening, not correctness defects. `24.6-F6-W3` already covers the `Single()` case alone.
  - Re-open trigger: Any of these sites fails with a bare `NullReferenceException` or `InvalidOperationException` in CI, or the seed query is observed duplicating relationships.
