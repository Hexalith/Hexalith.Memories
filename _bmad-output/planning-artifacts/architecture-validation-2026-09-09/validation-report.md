**FAIL — `_bmad-output/planning-artifacts/architecture.md` is unsafe as the current implementation architecture or controlling consistency spine.**

# Architecture Validation Report

**Validation date:** 2026-09-09  
**Mode:** read-only validation; no source document was changed  
**Next action:** **Update** — replace/distil the legacy artifact, resolve critical choices, then rerun validation

## Result

| Measure | Result |
|---|---|
| Overall gate | **FAIL / unsafe** |
| Consolidated findings | **17** — 6 critical, 6 high, 3 medium, 1 low, 1 informational |
| Raw reviewer observations | **44**, all represented in the crosswalk |
| Approval | Do not use for implementation handoff |

The architecture preserves useful intent—EventStore-acknowledged domain truth, Dapr Workflow
orchestration, rebuildable projections, Redis-native atomic deduplication, and adversarial isolation
testing—but it cannot make independently built units converge. Its load-bearing rules conflict with one
another, current code, and the 2026-09-08 product contract. Current implementation already demonstrates
several forks.

## Scope and method

This report merges only these five completed reviews; no additional research or target reread was used:

1. [`reviews/review-mechanical.md`](reviews/review-mechanical.md) — deterministic spine linter.
2. [`reviews/review-rubric.md`](reviews/review-rubric.md) — good-spine rubric walk.
3. [`reviews/review-technology-currentness.md`](reviews/review-technology-currentness.md) — repository-pin
   and first-party upstream currentness check.
4. [`reviews/review-adversarial-divergence.md`](reviews/review-adversarial-divergence.md) — independently
   built-unit divergence counterexamples.
5. [`reviews/review-data-integrity.md`](reviews/review-data-integrity.md) — data integrity, state ownership,
   and upstream contract drift.

Duplicates were merged by underlying decision failure. Consolidated severity is the highest justified by
any source review. This is static evidence: it does not assert that skipped or environment-dependent tests
were executed.

### Mechanical false-positive note

The mechanical linter returned `ok: false` with six low possible-template-token hits. All are intentional
runtime naming tokens—`{tenant}` and `{model-version}` at `architecture.md:75`, `:300`, `:697`, and
`:699`—not authoring placeholders; disposition **ignore**. The meaningful limitation is structural: the
legacy `D1–D31`/`ADR-IDA-001` forms do not expose the current linter's `AD-n` `Binds`, `Prevents`, and
`Rule` fields, so it could not mechanically prove decision completeness or precedence.

## Critical findings

### VAL-C01 — MemoryUnit commit, projection completion, and rebuild do not form one integrity contract

**Evidence.** The architecture requires EventStore acceptance before MemoryUnit projection and calls the
three projections rebuildable (`architecture.md:45`, `:98`, `:292`, `:582`), yet its ingest flow goes
straight to `IngestionWorkflow` (`:1598-1620`). Current file/URL endpoints do the same
(`IngestionEndpoints.cs:108-142`, `:303-320`); there is no ordinary MemoryUnit-create command and the
index inputs carry no EventStore source version. Verification stores presence booleans only
(`ConsistencyResult.cs:10-24`), and a missing projection becomes a warning before the workflow
unconditionally returns `Indexed` (`IngestionWorkflow.cs:433-465`, `:608-623`). The shipped repair path
enumerates derived stores, treats syntactic Redis as authority, and cannot rebuild an all-three-absent unit
(`EnumerateMemoryUnitIdsActivity.cs:21-40`; `RepairPlanCalculator.cs:15-44`;
`docs/operations/index-rebuild.md:19-22`, `:56-76`). Existing primitive tests do not prove the full
request→commit→same-version receipts→rebuild invariant.

**Risk.** Projection-only units cannot be replayed, and a two-of-three or stale unit can be reported
terminally indexed.

**Disposition: discuss, then Update code and spine together.** Bind the authoritative command/event,
aggregate identity, retained payload/reference, source version, durable scheduling handoff, versioned
projection receipts, sole `indexed` transition, authoritative enumerator, dedup bypass, and end-to-end
failure tests.

### VAL-C02 — Tenant-principal isolation is incompatible with process-wide backend credentials

**Evidence.** The architecture selects per-tenant Redis ACL users and tenant-scoped routing
(`architecture.md:71`, `:76`, `:280-284`, `:603`, `:607-617`) while D30 gives product code process-wide
keyed connections (`:619-648`). Current composition registers one singleton per backend and current search
and indexing consumers inject it directly (`ServiceDefaults/Extensions.cs:76-100`;
`SyntacticSearchService.cs:49-82`; `IndexSyntacticActivity.cs:25-54`); Kubernetes supplies one shared
credential (`server-deployment.yaml:46-59`). Tenant deletion also misses
`ingest-reserve:dedup:{tenant}:*`, allowing a rapidly recreated tenant to encounter stale reservations
(`DeleteTenantDataKeysActivity.cs:50-79`; `IngestDedupReservation.cs:36-48`). Collision/marker tests prove
data shaping, not the selected principal boundary.

**Risk.** A routing/key defect retains cross-tenant authority; separately implemented ACL provisioning can
also break consumers that have no tenant-aware resolver.

**Disposition: discuss.** Choose one enforceable connection-ownership model and bind provisioning, use,
caching, rotation, revocation, deletion/recreation, workflow fencing, and negative evidence for Redis and
FalkorDB.

### VAL-C03 — The document omits the current security and thesis-gate contract

**Evidence.** The target starts from C# 13 and 31 NFRs and certifies 31/31 coverage
(`architecture.md:32-37`, `:61`, `:1660-1666`); the current PRD owns C# 14 and NFR1–NFR36. It calls
authentication Phase 1.5/non-gating (`architecture.md:303-320`, `:1664-1666`) while current Server startup
requires authentication settings and an authenticated fallback policy. It also retains the old small
synthetic, best-single-axis, explicit-seed benchmark (`architecture.md:94-96`, `:170-198`, `:370-377`),
whereas the adopted PRD requires ≥50 representative real topics, a BM25+semantic control, frozen
independent labels, delta/aggregate guards, and deterministic graph auto-seeding from the top-five
syntactic plus top-five semantic candidates at depth ≤2. Current search skips graph when no start node is
provided (`HybridSearchService.cs:168-186`), and the shipped eight-query suite supplies explicit seeds.

**Risk.** A compliant team can omit active ingress security or certify a two-axis runtime path as the
three-axis thesis.

**Disposition: discuss, then Update.** Re-extract security, phase, graph-entry, and benchmark constraints
from the current PRD/addendum. Keep implementation gaps explicit; do not weaken the PRD to match this
legacy document.

### VAL-C04 — Mandatory decisions contradict one another and lack enforceable form

**Evidence.** The artifact calls D1–D31 implementation-blocking and says to follow all exactly
(`architecture.md:535-536`, `:1750-1759`). D15 requires actor ID `{actorType}-{tenantId}` (`:557`,
`:775-778`), but D24 and the actor table require tenant ID alone (`:342-347`, `:598`); current code uses
tenant-only IDs for rate limiting and a different tenant/case composite for counters. D30 says direct
Redis/FalkorDB APIs belong only in boundary projects (`:603-648`), while Server directly references and
uses those APIs and the Redis project is a compatibility facade. The document mixes requirements,
executable-looking examples, speculative structure, rationale, self-certification, and implementation
steps without a governing paradigm or rule precedence.

**Risk.** Independent units can create different durable actor state or relocate backend ownership in
opposite directions while both claim compliance.

**Disposition: discuss, then replace/distil.** Decide actor identity per ownership scope and whether D30
governs connection construction or API consumption. Express the few durable choices as short `AD-n`
contracts with `Binds`, `Prevents`, `Rule`, precedence, and automated enforcement; move history to the
memlog and code-owned detail to source/config.

### VAL-C05 — Licence facts are wrong and the claimed Dapr-boundary mitigation is unsupported

**Evidence.** The architecture calls FalkorDB AGPL and says the Dapr boundary mitigates the licence
(`architecture.md:65`, `:201-209`), but the deployed FalkorDB v4.12.0 server is SSPL v1. It also labels the
repository Apache 2.0 (`:1316-1329`), while root `LICENSE` and the current PRD specify MIT. Network/process
separation does not itself alter licence obligations, and the Apache-2.0 NFalkorDB client is distinct from
the server.

**Disposition: discuss.** Correct the facts, remove the unsupported mitigation statement, distinguish
client/server artefacts, and record an accountable legal/product decision before approval. Official
evidence: [FalkorDB v4.12.0 licence](https://github.com/FalkorDB/FalkorDB/blob/v4.12.0/LICENSE.txt),
[FalkorDB repository](https://github.com/FalkorDB/FalkorDB), and
[Redis licence matrix](https://redis.io/legal/licenses/).

### VAL-C06 — OpenBao and PostgreSQL image pins trail security-fix patch releases

**Evidence.** OpenBao 2.6.0 is pinned in CI, AppHost, Helm values, and smoke tests, while official 2.6.2
notes include security fixes. Access telemetry pins PostgreSQL 18.4, while 18.6 contains fixes and CVEs
since 18.4. D31 and the access-ledger text name the products but no image-pin authority, patch cadence, or
exposure-review trigger (`architecture.md:215-230`, `:650-677`). This does not assert that every CVE is
reachable.

**Disposition: open.** Assess exposure/upgrade compatibility; update digests or record time-bounded
exceptions, then make manifests the version authority. Official evidence:
[OpenBao 2.6 notes](https://openbao.org/community/release-notes/2-6-0/),
[OpenBao API compatibility](https://openbao.org/api-docs/next/), and
[PostgreSQL 18.6 notes](https://www.postgresql.org/docs/release/18.6/).

## High findings

### VAL-H01 — AI enrichment has two owners; mandatory Python Dapr Agents is absent and not officially GA

**Evidence.** D27/D28 assign enrichment to a Python `ai-agent`, call Dapr Agents GA, and show `/enrich`
invocation (`architecture.md:61-69`, `:256-270`, `:596-605`, `:1116-1232`, `:1514-1533`); another
normative example performs extraction in C# through `DaprConversationClient`. The repository chose the C#
path and has no `services/ai-agent`. Dapr's official status says Agents is “In development,” and its
package metadata uses a pre-alpha classifier. The proposed handwritten C#/Pydantic mirror also lacks
schema/route versioning, unknown-field policy, error envelope, idempotency, and retry ownership.

**Disposition: discuss.** Ratify one owner. If Python survives, require a generated/versioned contract,
deployment evidence, supported Python range, and maturity gate; otherwise move it to a dated open item.
Official evidence: [Dapr SDK status](https://docs.dapr.io/developing-applications/sdks/),
[Agents on PyPI](https://pypi.org/project/dapr-agents/),
[package metadata](https://github.com/dapr/dapr-agents/blob/main/pyproject.toml), and
[releases](https://github.com/dapr/dapr-agents/releases).

### VAL-H02 — The March dependency inventory is stale and can regress the repository

**Evidence.** The “Current Verified Versions (March 2026)” table and handoff pin Aspire 13.1.3, Dapr
1.17.6, NRedisStack 1.3.0, StackExchange.Redis 2.12.4, NFalkorDB 1.0.0, xunit.v3 3.2.2, and NSubstitute
5.3.0 (`architecture.md:464-480`, `:1250-1263`, `:1640-1643`, `:1761-1766`). Current repo authorities
specify SDK 10.0.400/C# 14, Aspire 13.5.3, Dapr SDK 1.18.5/runtime 1.18.2, NRedisStack 1.7.4,
StackExchange.Redis 3.1.31, NFalkorDB 1.2.0, xunit.v3 4.0.0, and NSubstitute 6.2.0. The Aspire Dapr toolkit
is a current prerelease, another status the target omits.

**Disposition: autofix in Update.** Make `global.json`, central package management, CI, and manifests the
authorities; keep only dated architectural compatibility constraints and preview-graduation triggers.

### VAL-H03 — The committed Google embedding model is retired and the provider rule is obsolete

**Evidence.** The stable-ID example and migration path use `text-embedding-004` and hypothetical `005`
(`architecture.md:104-118`, `:286-301`); Google shut down `text-embedding-004` on 2026-01-14. Current code
supports `gemini-embedding-001` and Ollama `qwen3-embedding:4b` with explicit dimensions and config-sourced
endpoints, contradicting D4's Google-only MVP rule.

**Disposition: autofix in Update.** Record the implemented provider/model/dimension matrix and preserve
reindexing for breaking changes. Official evidence:
[Gemini deprecations](https://ai.google.dev/gemini-api/docs/deprecations),
[`gemini-embedding-001`](https://ai.google.dev/gemini-api/docs/models/gemini-embedding-001), and
[embedding guidance](https://ai.google.dev/gemini-api/docs/embeddings).

### VAL-H04 — Case/Tenant mutation ownership and accepted-command handoff are incomplete

**Evidence.** Several paths correctly accept EventStore commands before projection, but Case membership
mutates Redis directly; tenant display name is folded aggregate state yet updates directly in Dapr state;
final tenant removal deletes the registry without an EventStore `Deleted` state/event. Command acceptance
and workflow scheduling are also separate calls with no specified crash-recovery handoff.

**Disposition: discuss.** Publish a mutation matrix covering authority, command/event, aggregate,
projection, idempotency, terminal state, and post-accept scheduling recovery. Decide which fields are
domain truth versus disposable read-model/config state.

### VAL-H05 — Current topology, environments, operations, and component inventory are false or fragmented seed

**Evidence.** The target alternates between AppHost and a Docker Compose Gate 3 although no root Compose
artifact exists. Its five-container topology includes the nonexistent Python service and omits current
EventStore, MCP, access-telemetry/clock, PostgreSQL, OpenBao, RBAC, and production/qualification overlays.
Its project counts/tree are obsolete. It leaves implemented REST/export and blue/green index migration
under Deferred/Open while AppHost pulls floating Redis/FalkorDB images against digest-pinned production
versions.

**Disposition: autofix in Update.** Use AppHost as the Phase 1 onboarding gate unless Compose gains a
separate owner/parity test. Point inventory to solution/release manifests, distil an environment and
operations matrix, convert implemented decisions to adopted/code-owned seed, and bind a tested local/prod
image policy. Official evidence: [Redis Stack 7.4 notes](https://redis.io/docs/latest/operate/oss_and_stack/stack-with-enterprise/release-notes/redisstack/redisstack-7.4-release-notes/)
and [FalkorDB releases](https://github.com/FalkorDB/FalkorDB/releases).

### VAL-H06 — The mandated MediatR/FluentValidation pipeline is not used by Server

**Evidence.** The pattern/enforcement sections make both libraries mandatory (`architecture.md:818-826`,
`:1279-1287`). Central management makes MediatR 14.2.0 and FluentValidation 12.1.1 available, but Server
does not reference or use them. A catalogue entry is not adoption.

**Disposition: discuss.** Ratify the compiled endpoint/domain validation design or authorize and fund the
pipeline, then align the enforcement table. Official registries:
[MediatR](https://api.nuget.org/v3-flatcontainer/mediatr/index.json) and
[FluentValidation](https://api.nuget.org/v3-flatcontainer/fluentvalidation/index.json).

## Compact appendix — medium, low, and informational

| ID | Severity | Finding and evidence | Disposition |
|---|---|---|---|
| VAL-M01 | Medium | Idempotency identity/order vary by ingress: REST uses tenant+case+URI/token, event ingress tenant+case+CloudEvent ID, and a fail-open Redis reservation precedes workflow dedup; the workflow updates a counter before the purported “first” check (`architecture.md:74`, `:188`, `:1250`; `IngestionWorkflow.cs:52-86`). | **Discuss:** bind identity, collision/replay, TTL/cleanup, and duplicate-neutral pre-check effects per ingress. |
| VAL-M02 | Medium | The target publishes stored `queued/indexing`, while the PRD binds operator `pending/projecting` with an explicit mapping to current `Queued/Indexing` (`architecture.md:100-119`). | **Autofix:** define one translation boundary across API/CLI/UI, Dapr status, telemetry, and persistence; do not silently rename V1. |
| VAL-M03 | Medium | Dapr Conversation isolates Server code, but the target overstates production providers as YAML-equivalent; official OpenAI/Mistral/Anthropic/Google AI components are Alpha v1 and need provider-specific metadata/secrets. | **Defer:** qualify maturity and require per-provider schema/integration evidence. [Official component catalogue](https://docs.dapr.io/reference/components-reference/supported-conversation/). |
| VAL-L01 | Low | Duplicate `workflowType`, `status: complete` self-certification, and the six intentional token-pattern linter hits are legacy/mechanical residue. | **Ignore:** replacement removes metadata residue; tokens are false positives. |
| VAL-I01 | Informational | Current .NET 10/C# 14, Aspire, Dapr, Redis/FalkorDB clients, and test packages exist and broadly fit. OpenBao 2.6 and PostgreSQL 18 remain valid lines. The defects are stale prose, maturity omissions, and old patch pins—not wholesale technology disappearance. | **Ignore as a defect; retain as positive evidence.** |

## Source-finding crosswalk

Every raw reviewer observation is represented after de-duplication.

| Consolidated | Original findings |
|---|---|
| VAL-C01 | Adversarial C2; data-integrity DI-01, DI-02, DI-03, DI-09 |
| VAL-C02 | Adversarial C1; data-integrity DI-06 |
| VAL-C03 | Rubric C2; adversarial H3/H4; data-integrity DI-04 |
| VAL-C04 | Rubric C1/H2/H5; adversarial H1 |
| VAL-C05 | Technology TC-01; rubric M4 |
| VAL-C06 | Technology TC-02 |
| VAL-H01 | Rubric H1; adversarial H2; technology TC-04 |
| VAL-H02 | Technology TC-03; rubric M1 |
| VAL-H03 | Technology TC-05 |
| VAL-H04 | Data-integrity DI-05 |
| VAL-H05 | Rubric H3/H4/M2; adversarial M1; technology TC-07 |
| VAL-H06 | Technology TC-06 |
| VAL-M01 | Data-integrity DI-07 |
| VAL-M02 | Data-integrity DI-08; rubric M3 |
| VAL-M03 | Technology TC-08 |
| VAL-L01 | Mechanical 1–6; rubric low tail |
| VAL-I01 | Technology TC-09 |

## Official technology sources

All links below were already captured by the technology-currentness reviewer and were accessed on
2026-09-09.

- **Platform:** [.NET 10/C# 14](https://dotnet.microsoft.com/en-us/download/dotnet/10.0),
  [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core),
  [Aspire.Hosting 13.5.3](https://www.nuget.org/packages/Aspire.Hosting/13.5.3),
  [CommunityToolkit Aspire Dapr](https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Dapr/).
- **Dapr:** [.NET SDK releases](https://github.com/dapr/dotnet-sdk/releases),
  [support policy](https://docs.dapr.io/operations/support/support-release-policy/),
  [Dapr.Client](https://www.nuget.org/packages/Dapr.Client/),
  [SDK/framework status](https://docs.dapr.io/developing-applications/sdks/),
  [Agents PyPI](https://pypi.org/project/dapr-agents/),
  [Agents metadata](https://github.com/dapr/dapr-agents/blob/main/pyproject.toml),
  [Agents releases](https://github.com/dapr/dapr-agents/releases),
  [Conversation components](https://docs.dapr.io/reference/components-reference/supported-conversation/).
- **Data/client packages:** [NRedisStack](https://www.nuget.org/packages/NRedisStack/),
  [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis/),
  [NFalkorDB versions](https://api.nuget.org/v3-flatcontainer/nfalkordb/index.json),
  [NFalkorDB documentation](https://docs.falkordb.com/getting-started/clients),
  [Kreuzberg versions](https://api.nuget.org/v3-flatcontainer/kreuzberg/index.json),
  [MediatR versions](https://api.nuget.org/v3-flatcontainer/mediatr/index.json),
  [FluentValidation versions](https://api.nuget.org/v3-flatcontainer/fluentvalidation/index.json).
- **Tests:** [xunit.v3](https://www.nuget.org/packages/xunit.v3/),
  [NSubstitute](https://www.nuget.org/packages/NSubstitute/),
  [Shouldly versions](https://api.nuget.org/v3-flatcontainer/shouldly/index.json).
- **Models:** [Gemini deprecations](https://ai.google.dev/gemini-api/docs/deprecations),
  [`gemini-embedding-001`](https://ai.google.dev/gemini-api/docs/models/gemini-embedding-001),
  [embedding guidance](https://ai.google.dev/gemini-api/docs/embeddings).
- **Servers/licensing:** [FalkorDB v4.12.0 licence](https://github.com/FalkorDB/FalkorDB/blob/v4.12.0/LICENSE.txt),
  [FalkorDB repository](https://github.com/FalkorDB/FalkorDB),
  [FalkorDB releases](https://github.com/FalkorDB/FalkorDB/releases),
  [Redis licences](https://redis.io/legal/licenses/),
  [Redis Stack 7.4 notes](https://redis.io/docs/latest/operate/oss_and_stack/stack-with-enterprise/release-notes/redisstack/redisstack-7.4-release-notes/).
- **Operations:** [OpenBao 2.6 notes](https://openbao.org/community/release-notes/2-6-0/),
  [OpenBao API compatibility](https://openbao.org/api-docs/next/),
  [OpenBao installation](https://openbao.org/docs/install/),
  [PostgreSQL 18.6 notes](https://www.postgresql.org/docs/release/18.6/),
  [PostgreSQL 18 release line](https://www.postgresql.org/docs/18/release.html).

## Gate action

**Next action: Update.** Do not approve the legacy artifact. Resolve the six critical decisions, distil a
short current spine from the existing reviews and brownfield authorities, make all remaining gaps explicit
with owners/revisit conditions, and rerun the mechanical, rubric, technology, adversarial, and
data-integrity lenses. The legacy architecture may remain historical input only.

**No-source-change note:** validation created/updated only this report and its standalone HTML mirror;
`architecture.md` and the five source reviews were not modified.
