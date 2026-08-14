# Sprint Change Proposal — Story 24.3 Verifier Residual Backlog Decisions

**Date:** 2026-08-04  
**Mode:** Batch  
**Status:** Approved — canonical backlog registered  
**Trigger:** Epic 24 retrospective Action Item 3 and the Story 24.3 review residuals in `deferred-work.md`  
**Change classification:** Moderate direct adjustment with backlog reorganization  
**Verified baseline:** `main@e902181dcdce599187e74fd2c3c9b12f995dcc18`

This approved correction converts five unstructured Story 24.3 review bullets into four explicit
backlog decisions. It does not reopen completed Story 24.3 and does not implement source changes.
Administrator approval authorized the planning, architecture, deferred-ledger, and sprint-registry
transaction recorded below; each implementation story remains subject to normal selection and
evidence gates.

## 1. Issue Summary

Story 24.3 completed its bounded outcome: it ratified the physical-isolation target, replaced
pairwise deep scans with target metadata/cursor checks, removed the runtime input-validation
self-test, and added semantic `tenantId` markers. Its focused verifier suite is currently green at
16/16. Review nevertheless left five raw deferred bullets without structured IDs, owners, statuses,
or registered backlog homes:

1. graph content-level leakage evidence;
2. graph/search integration tests whose names exceed their behavior;
3. vector dimensions compared to each other but not to tenant configuration;
4. semantic scans that include staging and legacy key families and can report false failures; and
5. missing-marker diagnostics that share destructive remediation wording with confirmed foreign
   markers.

The first two bullets describe one outcome and should be owned by one graph-evidence story. The
remaining bullets are independent outcomes. Leaving them as prose conflicts with the Epic 24
retrospective success criterion that every residual receive a story home or explicit accepted-debt
disposition.

### 1.1 Reverification ledger

The claims below were rechecked against the verified baseline. Full commands are in Section 8.

| Claim | Observation | Verdict |
| :---- | :---------- | :------ |
| Runtime `GraphIsolation` is structural existence evidence only. | `CheckGraphIsolationAsync` issues `GRAPH.LIST` and verifies only that the target database exists. It issues no content query. | `confirmed` |
| The two graph/search integration test names and comments exceed their current behavior. | Both tests provision two tenants and call the verify endpoint; neither ingests identical graphs, traverses, searches, or asserts foreign-content absence. | `confirmed` |
| Semantic dimensions are compared to each other but not tenant configuration. | The verifier compares raw semantic dimensions with natural-language semantic dimensions. `ITenantEmbeddingConfigProvider` is registered but is not injected into the verifier. | `confirmed` |
| Broad semantic prefix scans overlap staging and legacy semantic key families. | `{tenant}:vec:*` also contains raw staging and legacy nested-NL keys; `{tenant}:vecnl:*` also contains NL staging keys. Migration staging writers do not write a `tenantId` field. | `confirmed` |
| Prefix-only family classification would misclassify collision-shaped opaque memory-unit IDs. | `ValidateMemoryUnitId` requires only nonblank text, so legitimate identifiers may contain `staging:`, `nl:`, version-like, or chunk-like colon suffixes. | `confirmed` |
| Missing and foreign markers share one mismatch collection and aggregate remediation. | Both enter the same mismatch collection, and `SemanticIsolation` recommends removing mismatched target-prefix hashes. | `confirmed` |
| Story identities 24.6-24.9 are unregistered but reserved by an approved proposal. | They are absent from `epics.md` and `sprint-status.yaml`, but the approved 2026-08-03 proposal reserves those unregistered identities for physical-isolation qualification, enforcement, migration, and runtime evidence. | `confirmed` |
| The current focused verifier suite is green. | Server test project build passed with 0 warnings/errors; `TenantIsolationVerifierTests` passed 16/16. | `confirmed` |

These verdicts confirm the residuals against current code and planning. They do not rewrite Story
24.3's historical implementation record.

## 2. Impact Analysis

### 2.1 Epic impact

| Epic or area | Impact |
| :----------- | :----- |
| Epic 24 | Reopen the epic registry from `done` to `in-progress` without reopening Story 24.3. Add four bounded verifier/evidence stories. |
| Epic 21 | Stories 21.3 and 21.9 remain completed historical sources for active, staging, and legacy semantic namespaces. Their implementation is not reopened. |
| Physical-isolation follow-up | Re-key the four held, unregistered identities from the approved 2026-08-03 proposal so verifier correctness precedes enforcement and deployment evidence in monotonic order. |
| Other epics | No epic is invalidated and no new epic is required. Future tenant-sensitive work continues to attach Story 20.2 authorization negatives and Story 24.3 verifier/marker evidence where applicable. |

### 2.2 Story impact and sequencing

| Story | Independent outcome | Owner | Dependency | Size |
| :---- | :------------------ | :---- | :--------- | :--- |
| 24.6 | Approved two-checkpoint umbrella: executable graph content-isolation evidence plus post-OpenBao-restart Dapr actor-proxy reconnection evidence. | Murat / Test Architect, Developer | Story 24.3 historical evidence; second-pass Decision D2 | M |
| 24.7 | Both semantic index families are verified independently against the requested tenant's configured dimensions. | Winston / Architect, Developer | Story 24.3; existing `ITenantEmbeddingConfigProvider` | S |
| 24.8 | Active semantic key families are classified exactly so staging and legacy hashes cannot create marker false positives. | Developer, Murat / Test Architect | Stories 21.3, 21.9, and 24.3 as history | S |
| 24.9 | Missing and foreign active semantic markers fail closed with distinct, non-destructive operator semantics. | Winston / Architect, Murat / Test Architect, Developer | Story 24.8 | S |

Stories 24.6, 24.7, and 24.8 may be selected independently. Story 24.9 follows 24.8 because its
diagnostics consume the exact active-key classification. Physical-isolation qualification and
enforcement follow as held Stories 24.10-24.13 after their existing registration prerequisites are
satisfied.

### 2.3 Artifact conflicts

| Artifact | Current conflict | Required approved change |
| :------- | :--------------- | :----------------------- |
| `epics.md` | Epic 24 ends at Story 24.5; no story owns the residuals. | Add Stories 24.6-24.9 and the held-identity re-key note. |
| Backlog story files | No dedicated Story 24.6-24.9 artifacts exist. | Create four `Status: backlog` story files with the canonical `## Dev Notes` and `### Epic AC Verification` sections before registration. |
| `architecture.md` | D29 distinguishes physical enforcement from prefix evidence but does not classify graph proof, configured dimensions, key families, or marker remediation. | Add verifier-evidence semantics under D29. |
| `deferred-work.md` | Five Story 24.3 bullets predate the structured active-entry schema. | Replace them with four carried-forward entries mapped to Stories 24.6-24.9. |
| `sprint-status.yaml` | Epic 24 is `done`; Action Item 3 is `open`; no residual story rows exist. | Set Epic 24 `in-progress`, add four `backlog` rows and an Epic 24 execution order, then mark the action `done` only after all atomic edits land. |
| `epic-24-context.md` | Generated context lists only Stories 24.1-24.5. | Regenerate or update it after canonical epic registration. |
| Approved 2026-08-03 proposal | Held Stories 24.6-24.9 use identities needed for the earlier verifier-correctness work. | Preserve that proposal as history; record the explicit old-to-new identity map in current planning. |
| PRD | FR40 and NFR8 already require automated tenant verification and real graph leakage evidence. | No requirement change. |
| UX specification | Existing feedback grammar already separates critical isolation failure, degraded evidence, schema mismatch, and recovery actions. | No UX change. Future surfaces consume the clarified architecture semantics. |

### 2.4 Technical and operational impact

This course correction changes planning only. The future stories are expected to touch the tenant
verifier, semantic key classification, verifier DI, focused unit/integration tests, and operator
guidance. They do not authorize Redis ACL rollout, tenant data deletion, broad data migration, a V1
JSON contract break, or changes to authorization behavior.

Every implementation story is tenant-scope-sensitive and must name the affected verifier,
configuration, Redis key/index, graph, endpoint, CLI/operator, and test surfaces that it changes.
Completion must attach focused negative test names, exact commands, and results. Broad-suite,
build-only, or happy-path evidence is insufficient.

## 3. Recommended Approach

### 3.1 Selected path: Direct Adjustment

Add four bounded stories inside Epic 24, structure the four deferred decisions, and re-key the held
physical-isolation candidates before any of them are registered.

This path preserves Story 24.3's valid completed slice while putting each residual behind one
independently demonstrable outcome. It also keeps runtime structural verification distinct from
deployment-shaped evidence and avoids treating test names as proof.

### 3.2 Alternatives evaluated

| Option | Viability | Effort | Risk | Assessment |
| :----- | :-------- | :----- | :--- | :--------- |
| Direct adjustment | Viable, recommended | Medium implementation; low planning | Medium | Four focused slices close the exact evidence and diagnostic gaps without disturbing completed behavior. |
| Roll back Story 24.3 | Not viable | High | High | It would restore pairwise deep scans and remove valid target-marker evidence without resolving any residual. |
| Reduce or redefine MVP | Not viable | High governance cost | High | NFR8 is a hard zero-leak requirement; the residuals refine evidence quality and remediation, not the product thesis. |
| Accept all four as debt | Not recommended | Low immediate | High | False-positive and destructive-remediation ambiguity can misdirect operators; graph and dimension gaps weaken claims required by current PRD text. |

### 3.3 Timeline and priority

Register the four stories atomically, then allow 24.6-24.8 to proceed in parallel. Complete 24.8
before 24.9. The physical-isolation chain may be registered later as 24.10-24.13 when each candidate
has its prerequisite and executable producer. No current MVP delivery date changes; Epic 24 returns
to `in-progress` on the excluded audit-remediation track.

## 4. Detailed Change Proposals

### 4.1 Epic 24 story registration

**Artifact:** `_bmad-output/planning-artifacts/epics.md`  
**Section:** Epic 24, after Story 24.5

**OLD:**

> Epic 24 ends with Story 24.5. Story 24.3 is complete and its review residuals have no story
> definitions in the epic.

**NEW:** Add the four story definitions below.

The approved registration transaction must also create these dedicated backlog artifacts:

- `_bmad-output/implementation-artifacts/24-6-graph-content-level-tenant-isolation-evidence.md`
- `_bmad-output/implementation-artifacts/24-7-tenant-configured-vector-dimension-verification.md`
- `_bmad-output/implementation-artifacts/24-8-semantic-isolation-key-family-classification.md`
- `_bmad-output/implementation-artifacts/24-9-non-destructive-tenant-marker-diagnostics.md`

Each file must use the canonical `## Dev Notes` → `### Epic AC Verification` heading structure.
The deeper heading levels below reflect this proposal's nesting only.

#### Story 24.6: Graph Content-Level Tenant Isolation Evidence

**Status:** review. **Owner:** Murat / Test Architect + Developer.

> **Approved clarification (2026-08-13):** Second-pass Decision D2 (2026-08-12), propagated here
> under fifth-pass Decision F5-D1, supersedes this proposal's one-independent-outcome success criterion
> for Story 24.6 only. Story 24.6 may close two ratified checkpoints: C1 graph content-level
> tenant-isolation evidence and C2 the post-OpenBao-restart Dapr actor-proxy reconnection CI repair.
> Stories 24.7-24.9 retain the original criterion unchanged.

As an operator,
I want executable proof that graph content remains tenant-local when identifiers collide,
So that structural database existence is not mistaken for NFR8 leakage evidence.

**Acceptance Criteria:**

1. **Given** tenant A and tenant B contain identical node identifiers, identical graph shapes, and
   colliding edge identifiers but tenant-distinct payload markers, **when** each tenant is traversed
   through its authenticated tenant context, **then** every returned node and edge belongs to the
   requested tenant's seeded fixture and zero foreign markers appear.
2. **Given** the existing integration tests named
   `VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes` and
   `VerifyTenant_SearchFromOtherContext_ZeroResultsAcrossAllAxes`, **when** this story completes,
   **then** the graph test executes and asserts its collision-shaped scenario, while the redundant
   all-axis test is removed after the current axis-specific cross-tenant search tests are cited and
   verified.
3. **Given** runtime `GraphIsolation` checks only target database existence, **when** verifier and
   operator documentation describe the result, **then** they label it structural evidence and cite
   the focused integration command for content-level leakage proof; they do not claim `GRAPH.LIST`
   proves content isolation.
4. **Given** the integration lane cannot run, **when** the story is reviewed, **then** it remains
   blocked or records an accepted blocker with owner, consequence, and reopen trigger; unit mocks or
   comments cannot substitute for NFR8 graph evidence.

##### Dev Notes

###### Historical Context Classification

| Prior influence | Classification | Use in this story |
| :-------------- | :------------- | :---------------- |
| Story 24.3 | `historical-reference-only` | Preserve its structural verifier and marker evidence; do not expand or reopen its completed slice. |
| Current graph/search integration test bodies | `current-baseline/problem` | Their names/comments identify the desired scenarios, but their present bodies are not proof. |
| PRD NFR8 graph fixture | `current-narrow-pattern` | Reuse the exact identical-structure/colliding-ID outcome. |

###### Slice Proof

- **One outcome:** rerunnable graph content-isolation evidence.
- **Independent demonstration:** run only the named graph/tenant integration cases and inspect the
  tenant-distinct result assertions.
- **Excluded:** Redis ACL enforcement, graph resource isolation, vector checks, and semantic-marker
  remediation.

###### Epic AC Verification

| Exact inherited claim | Rerunnable evidence | Observed result | Verdict |
| :-------------------- | :------------------ | :-------------- | :------ |
| PRD NFR8: "Graph-specific test: create identical graph structures in tenant A and B, traverse from tenant A, verify zero nodes from tenant B appear even if edge IDs collide" | V1 | The requirement is current; the named tests exist but do not yet construct or assert that fixture. | `confirmed` |
| Story 24.3 residual: "`CheckGraphIsolationAsync` only verifies the target FalkorDB database appears in `GRAPH.LIST`" | V1 | Current verifier source issues only `GRAPH.LIST` for `GraphIsolation`. | `confirmed` |

#### Story 24.7: Tenant-Configured Vector Dimension Verification

**Status:** backlog. **Owner:** Winston / Architect + Developer.

As an operator,
I want semantic index dimensions checked against the requested tenant's embedding configuration,
So that two equally wrong indexes cannot pass isolation verification.

**Acceptance Criteria:**

1. **Given** the requested tenant's configured dimensions and `FT.INFO` for raw and
   natural-language semantic indexes, **when** verification runs, **then** each index is compared
   independently with the tenant configuration and the check passes only when all available values
   agree.
2. **Given** both indexes report the same dimension but the tenant configuration reports a different
   dimension, **when** verification runs, **then** `SemanticIsolation` fails with expected and actual
   dimensions and tenant-scoped reindex guidance.
3. **Given** tenant A and tenant B have different configured dimensions, **when** tenant A is
   verified, **then** only tenant A's configuration is requested and tenant B's value cannot satisfy
   or fail tenant A's check.
4. **Given** the tenant configuration source is unavailable or returns invalid dimensions, **when**
   verification runs, **then** the check fails closed with actionable backend/configuration guidance
   instead of silently falling back to raw-versus-NL equality.

##### Dev Notes

###### Historical Context Classification

| Prior influence | Classification | Use in this story |
| :-------------- | :------------- | :---------------- |
| Story 24.3 | `historical-reference-only` | Preserve its FT.INFO parsing and fail-closed check contract; do not treat pair equality as sufficient. |
| `ITenantEmbeddingConfigProvider` | `current-narrow-pattern` | Reuse the tenant-scoped cached configuration source already consumed by search and tenant endpoints. |
| Raw-versus-NL equality alone | `current-baseline/problem` | Retain it as a secondary consistency assertion, not the source of truth. |

###### Slice Proof

- **One outcome:** verifier dimensions agree with the requested tenant's configuration.
- **Independent demonstration:** focused verifier tests cover correct, equally-wrong, one-wrong,
  cross-tenant-config, and config-unavailable cases.
- **Excluded:** provider migration orchestration, index recreation, marker scanning, and ACL routing.

###### Epic AC Verification

| Exact inherited claim | Rerunnable evidence | Observed result | Verdict |
| :-------------------- | :------------------ | :-------------- | :------ |
| Story 24.3 residual: "Story 24.3 compares raw semantic and natural-language semantic dimensions to each other, but an equally wrong pair can still pass" | V2 | Current source compares only `rawDimensions` and `naturalLanguageDimensions`; the verifier has no configuration-provider dependency. | `confirmed` |
| Existing source capability: "Provides tenant embedding configuration with bounded per-process caching." | V2 | `ITenantEmbeddingConfigProvider.GetAsync(tenantId, ...)` exists and is registered. | `confirmed` |

#### Story 24.8: Semantic Isolation Key-Family Classification

**Status:** backlog. **Owner:** Developer + Murat / Test Architect.

As an operator,
I want tenant-marker scans limited to exact active semantic key families,
So that migration staging and legacy hashes do not create false isolation failures.

**Acceptance Criteria:**

1. **Given** active raw base/chunk hashes, active current-NL hashes, raw/NL migration staging hashes,
   and legacy nested-NL hashes, **when** semantic key classification runs, **then** every shape has one
   explicit classification and only active raw base/chunk and current-NL hashes enter the active
   tenant-marker scan.
2. **Given** markerless staging or legacy hashes coexist with healthy active hashes, **when** tenant
   verification runs, **then** those non-active families do not fail `SemanticIsolation`, while a
   markerless or foreign-marked active hash still fails closed.
3. **Given** memory-unit identifiers are opaque and may contain colon-delimited text resembling
   `staging:`, `nl:`, a version, or a chunk suffix, **when** classification runs, **then** canonical
   namespace provenance and record shape—not a prefix/suffix shortcut—decide family membership;
   collision-shaped tests keep legitimate active keys active, and unresolved ambiguity is reported
   as an evidence-classification gap rather than an invented marker mismatch.
4. **Given** a future semantic namespace or Story 21.9 migration state is introduced, **when**
   verifier, schema, and migration tests run, **then** an unknown family fails the classification
   guard and the verifier does not delete, mutate, or treat staging state as active evidence.

##### Dev Notes

###### Historical Context Classification

| Prior influence | Classification | Use in this story |
| :-------------- | :------------- | :---------------- |
| Stories 21.3 and 21.9 | `current-narrow-pattern` | Reuse their canonical legacy/current/staging namespace definitions and migration ownership. |
| Story 24.3 | `historical-reference-only` | Preserve fail-closed active marker checks; correct only family selection. |
| Broad `keyPrefix + "*"` semantic scan | `current-baseline/problem` | Replace with exact classification; do not copy it into new verifier paths. |
| Prefix-only staging/legacy parsing | `anti-template` | Use only to reproduce ambiguity because opaque identifiers can contain reserved-looking colon text. |

###### Slice Proof

- **One outcome:** exact active semantic key-family membership.
- **Independent demonstration:** focused classifier/verifier tests seed all five key shapes plus
  opaque-ID collision shapes and prove the expected inclusion/exclusion matrix.
- **Excluded:** deciding missing-marker wording, backfilling data, vector dimensions, and graph proof.

###### Epic AC Verification

| Exact inherited claim | Rerunnable evidence | Observed result | Verdict |
| :-------------------- | :------------------ | :-------------- | :------ |
| Story 24.3 residual: "The raw-semantic scan pattern `{tenantId}:vec:*` (and NL `{tenantId}:vecnl:*`) also match embedding-migration staging hashes ... and un-migrated legacy `{tenantId}:vec:nl:...` hashes" | V3 | Prefix constants and the verifier's `keyPrefix + "*"` scan reproduce the overlap. | `confirmed` |
| Story 24.3 residual: staging writers produce hashes "without a `tenantId` field" | V3 | Both staging write entry lists omit `tenantId`; the migration marker itself has a separate tenant field. | `confirmed` |

#### Story 24.9: Non-Destructive Tenant-Marker Diagnostics

**Status:** backlog. **Owner:** Winston / Architect + Murat / Test Architect + Developer.

As an operator,
I want missing and foreign tenant markers reported with distinct, safe recovery guidance,
So that incomplete evidence is not mislabeled as confirmed leakage or remediated by broad deletion.

**Acceptance Criteria:**

1. **Given** an active semantic hash has a foreign non-empty `tenantId`, **when** verification runs,
   **then** `SemanticIsolation` fails and the details identify confirmed marker mismatch/possible
   contamination, the exact key, expected tenant, and observed tenant without exposing payload data.
2. **Given** an active semantic hash has no `tenantId`, **when** verification runs, **then**
   `SemanticIsolation` remains fail-closed but the details classify the result as incomplete evidence,
   not confirmed cross-tenant leakage.
3. **Given** either failure, **when** remediation is returned, **then** it directs the operator to
   inspect/quarantine the named key and run tenant-scoped marker repair or reindex after provenance
   verification; it never instructs blanket prefix deletion. Confirmed foreign markers and missing
   markers receive different guidance.
4. **Given** the existing V1 result shape, **when** this story completes, **then** distinct semantics
   are expressed and pinned through `Details` and `Remediation` without a breaking JSON-contract
   change. A future machine-readable issue taxonomy requires a separate versioned contract story.

##### Dev Notes

###### Historical Context Classification

| Prior influence | Classification | Use in this story |
| :-------------- | :------------- | :---------------- |
| Story 24.3 | `historical-reference-only` | Preserve its fail-closed marker evidence while correcting diagnostic meaning. |
| `remove mismatched target-prefix hashes` remediation | `anti-template` | Do not reuse broad/destructive guidance for an unproven ownership mismatch. |
| Existing V1 `Details`/`Remediation` fields | `current-narrow-pattern` | Preserve the additive-compatible contract and pin distinct operator wording in tests. |

###### Slice Proof

- **One outcome:** safe, distinct marker diagnosis and recovery semantics.
- **Independent demonstration:** focused verifier and serialization/endpoint tests show foreign,
  missing, and healthy active markers with exact absence of blanket delete guidance.
- **Excluded:** executing repair, bulk backfill, changing V1 JSON shape, and classifying non-active
  namespaces owned by Story 24.8.

###### Epic AC Verification

| Exact inherited claim | Rerunnable evidence | Observed result | Verdict |
| :-------------------- | :------------------ | :-------------- | :------ |
| Story 24.3 residual: "the verifier now treats a missing marker like a foreign one and emits the destructive remediation `remove mismatched target-prefix hashes`" | V4 | Missing markers enter the mismatch list, and the aggregate semantic failure uses the quoted removal guidance. | `confirmed` |
| Contract claim: `TenantIsolationCheckResult` exposes "additional details" and "actionable operator guidance on failure." | V4 | V1 exposes nullable `Details` and `Remediation`; no issue-code field exists. | `confirmed` |

### 4.2 Held physical-isolation identity re-key

**Artifact:** canonical Epic 24 planning plus this proposal's forward-reference map  
**Rationale:** the 2026-08-03 candidates are approved but explicitly unregistered. Re-keying before
registration preserves history and creates monotonic verifier-correctness → enforcement sequencing.

| 2026-08-03 held identity | New held identity | Outcome unchanged |
| :----------------------- | :---------------- | :---------------- |
| 24.6 | 24.10 | Qualify and select one Redis ACL/credential/routing strategy. |
| 24.7 | 24.11 | Provision/rotate per-tenant credentials and enforce tenant-scoped Redis connections. |
| 24.8 | 24.12 | Migrate existing tenants/data with resumable cutover and rollback. |
| 24.9 | 24.13 | Produce hash-bound runtime physical-isolation evidence for a named profile. |

The approved 2026-08-03 proposal remains an immutable decision record. New canonical planning must
cite this map instead of silently reusing the old candidate IDs. Stories 24.10-24.13 remain held and
unregistered until their complete story definitions, owners, prerequisites, and executable producers
satisfy the existing registration gate.

### 4.3 Architecture clarification under D29

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`  
**Section:** D29 / physical tenant isolation and verifier evidence

**OLD:**

> Redis physical isolation target is per-tenant ACL users plus tenant-scoped backend resolution.
> Prefix-only naming is insufficient as a security boundary; verifier evidence must prove target
> tenant storage and metadata without pairwise deep scans.

**NEW:** Preserve the text above and add:

> **Verifier evidence semantics:** `GraphIsolation` database existence is structural evidence, not
> graph content-leakage proof; NFR8 content proof comes from a real identical-structure/colliding-ID
> traversal fixture. Semantic index dimensions are valid only when both index families independently
> match the requested tenant's configuration. Active semantic marker scans use exact, collision-safe
> key-family classification based on canonical namespace provenance and record shape—not reserved-looking
> string prefixes—and exclude migration staging and legacy namespaces from active evidence. Unresolved
> family provenance is an evidence-classification gap, not an invented marker mismatch. A foreign
> marker is possible contamination; a missing active marker is incomplete evidence. Both fail closed,
> but neither authorizes blanket deletion. Recovery is key-specific inspection/quarantine followed by
> tenant-scoped marker repair or reindex after provenance verification.

This clarification does not claim physical isolation is implemented and does not weaken NFR8.

### 4.4 Deferred-work decisions

**Artifact:** `_bmad-output/implementation-artifacts/deferred-work.md`  
**Section:** Deferred from Story 24.3

**OLD:** Five raw `source_spec` / `summary` / `evidence` bullets without structured IDs.

**NEW:** Replace them with these four structured dispositions while retaining the original evidence
text:

| ID | Status | Backlog home | Owner | Reopen trigger |
| :- | :----- | :----------- | :---- | :------------- |
| `24.3-GRAPH-CONTENT-EVIDENCE` | `carried-forward` | Story 24.6 | Murat / Test Architect, Developer | Any claim that `GRAPH.LIST`, the current test names, or unit mocks prove graph content isolation; or Story 24.6 selection. |
| `24.3-VECTOR-DIMENSION-SOURCE` | `carried-forward` | Story 24.7 | Winston / Architect, Developer | Any verifier assurance based only on raw/NL equality; or Story 24.7 selection. |
| `24.3-SEMANTIC-KEY-FAMILY` | `carried-forward` | Story 24.8 | Developer, Murat / Test Architect | Any migration/legacy tenant reports false `SemanticIsolation`; any new semantic namespace or prefix-only assumption about opaque IDs; or Story 24.8 selection. |
| `24.3-MARKER-REMEDIATION` | `carried-forward` | Story 24.9 | Winston / Architect, Murat / Test Architect, Developer | Any operator guidance recommends broad deletion, any missing marker is reported as confirmed leakage, or Story 24.9 selection. |

Each block must include `Source story`, `Target artifact`, `Rationale`, and `Re-open trigger` fields
required by the active-entry schema. Scheduling is not resolution: entries remain
`carried-forward` until their owning stories pass.

### 4.5 Sprint registry

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD:**

```yaml
epic-24: done
24-3-physical-tenant-isolation-and-verifier-scaling: done
```

The matching Epic 24 action item is `open`.

**NEW after approval and atomic canonical registration:**

```yaml
story_execution_order:
  epic-24:
    reason: "Verifier correctness and evidence semantics precede physical-isolation enforcement evidence; Stories 24.6-24.8 are independent, and Story 24.9 depends on Story 24.8."
    order:
      - 24-6-graph-content-level-tenant-isolation-evidence
      - 24-7-tenant-configured-vector-dimension-verification
      - 24-8-semantic-isolation-key-family-classification
      - 24-9-non-destructive-tenant-marker-diagnostics

development_status:
  epic-24: in-progress
  24-6-graph-content-level-tenant-isolation-evidence: backlog
  24-7-tenant-configured-vector-dimension-verification: backlog
  24-8-semantic-isolation-key-family-classification: backlog
  24-9-non-destructive-tenant-marker-diagnostics: backlog
```

Preserve all completed Story 24.1-24.5 and retrospective rows. Advance `last_updated` to the actual
mutation date. Mark the retrospective action `done` only in the same change that registers all four
stories and structured deferred dispositions; its comment must state that implementation remains
backlog.

### 4.6 Epic context

Regenerate `_bmad-output/implementation-artifacts/epic-24-context.md` after `epics.md` changes so its
story list, dependencies, D29 semantics, and evidence gates include Stories 24.6-24.9. Do not hand-edit
generated content if the repository compiler is available.

### 4.7 PRD and UX

No PRD or UX edits are proposed. FR40 and NFR8 already supply the product requirement. UX already
requires feedback to state what happened, affected scope, seriousness, and recovery, and already
distinguishes critical isolation failure from degraded/schema-mismatch evidence.

## 5. Implementation Handoff

### 5.1 Scope and recipients

**Classification:** Moderate — Product Owner / Developer backlog reorganization with Architect and
Test Architect review.

| Recipient | Responsibility |
| :-------- | :------------- |
| Product Owner | Apply the Epic 24, sprint-registry, action-ledger, and held-identity edits atomically after approval. |
| Solution Architect | Add and review the D29 verifier-evidence semantics; ensure Stories 24.7 and 24.9 preserve fail-closed behavior without overstating leakage. |
| Test Architect | Own Story 24.6 evidence design and review all four slice-proof commands and negative cases. |
| Developer | Implement only a selected story, preserve V1 contracts unless explicitly scoped, and attach focused tenant-negative evidence. |
| Platform/Security owners | Continue to own held physical-isolation Stories 24.10-24.13; this proposal does not select or implement them. |

### 5.2 Ordered handoff

1. On approval, create the four backlog story files and update `epics.md`, `architecture.md`,
   `deferred-work.md`, `sprint-status.yaml`, and `epic-24-context.md` atomically.
2. Run planning/story-scope, structured-deferred-entry, YAML, and whitespace checks.
3. Select Stories 24.6-24.8 independently according to capacity; 24.9 follows 24.8.
4. For each implementation, attach Story 20.2 denial-before-dependency evidence and Story 24.3
   fail-closed/tenant-marker evidence where the changed surface makes them applicable.
5. Keep Stories 24.10-24.13 held until their approved registration gates are satisfied.

### 5.3 Success criteria

The correction is complete when:

- all five raw residual bullets map to exactly four structured `carried-forward` entries;
- Stories 24.7-24.9 are registered with one independent outcome, an owner, prerequisites, exact
  acceptance evidence, historical classification, Slice Proof, and Epic AC Verification; Story 24.6
  uses the approved two-checkpoint umbrella from second-pass Decision D2, propagated under F5-D1;
- the held physical-isolation candidate map is 24.10-24.13 everywhere new planning cites it;
- Story 24.3 remains `done` and unchanged as a historical implementation record;
- Epic 24 and sprint status agree, and Action Item 3 is `done` only for the backlog-decision outcome;
- architecture distinguishes structural graph evidence, content proof, dimension authority, key
  families, missing evidence, confirmed mismatch, and non-destructive recovery;
- focused verifier tests remain green, and each future story adds its declared negative evidence;
  and
- `git diff --check` plus the repository's narrow planning/schema guards pass.

## 6. Change Navigation Checklist Result

### Section 1 — Trigger and context

- [x] 1.1 Story 24.3 and Epic 24 retrospective Action Item 3 identified.
- [x] 1.2 Technical/evidence limitations categorized precisely.
- [x] 1.3 Current source, tests, planning, and focused executable evidence collected.

### Section 2 — Epic impact

- [x] 2.1 Completed Story 24.3 remains valid; Epic 24 requires new backlog ownership.
- [x] 2.2 Four bounded stories and a held-candidate re-key are specified.
- [x] 2.3 Epic 21 namespace history and physical-isolation follow-up dependencies reviewed.
- [N/A] 2.4 No epic becomes obsolete and no new epic is needed.
- [x] 2.5 Verifier correctness is sequenced before deployment assurance.

### Section 3 — Artifact conflicts

- [x] 3.1 PRD reviewed; FR40/NFR8 remain sufficient.
- [x] 3.2 Architecture D29 clarification specified.
- [x] 3.3 UX reviewed; existing state/recovery grammar is sufficient.
- [x] 3.4 Epics, deferred ledger, sprint registry, generated context, source, and tests assessed.

### Section 4 — Path evaluation

- [x] 4.1 Direct adjustment is viable; medium implementation effort and medium risk.
- [x] 4.2 Rollback is not viable.
- [x] 4.3 MVP reduction/redefinition is not viable or necessary.
- [x] 4.4 Direct adjustment selected with explicit rationale.

### Section 5 — Proposal components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP impact, action plan, and dependencies documented.
- [x] 5.5 Moderate-change handoff roles defined.

### Section 6 — Final review and handoff

- [x] 6.1 All applicable checklist sections addressed.
- [x] 6.2 Proposal checked against current source and planning evidence.
- [x] 6.3 Administrator explicitly approved the proposal on 2026-08-04.
- [x] 6.4 Canonical story, epic, architecture, deferred-ledger, context, and sprint-status mutations were applied atomically after approval.
- [x] 6.5 Next-step responsibilities and success criteria are defined.

## 7. Approval State and Workflow Log

**Current state:** Approved and handed off. Canonical backlog, architecture, deferred-ledger,
epic-context, and sprint-status records are registered. Source/runtime implementation remains
unselected backlog work and is not authorized by this planning correction alone.

- 2026-08-04: Administrator invoked `bmad-correct-course` for Epic 24 Action Item 3.
- 2026-08-04: Required PRD, epics, architecture, UX, project context, Story 24.3, retrospective,
  deferred ledger, sprint registry, approved 2026-08-03 proposal, current source, and tests reviewed.
- 2026-08-04: Focused server test build passed with 0 warnings/errors; verifier tests passed 16/16.
- 2026-08-04: Batch proposal drafted with four story homes and an explicit held-identity re-key.
- 2026-08-04: Administrator approved the proposal.
- 2026-08-04: Stories 24.6-24.9 were created and registered; Epic 24 returned to `in-progress`;
  D29, deferred work, Epic 24 context, execution order, and retrospective Action Item 3 were updated.
- 2026-08-04: Story-scope and tenant-evidence gates passed for all four artifacts; sprint YAML
  assertions passed; deferred-ledger parser tests passed 2/2; the focused verifier build passed with
  0 warnings/errors and its tests passed 16/16; diff checks reported no whitespace errors.

## 8. Rerunnable Evidence

Run from the repository root at the verified baseline or re-record the new HEAD and observations.

**V0 — Focused verifier build and tests**

```bash
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -parallel none -class Hexalith.Memories.Server.Tests.Tenants.TenantIsolationVerifierTests
```

Observed 2026-08-04: build succeeded with 0 warnings and 0 errors; 16 tests passed, 0 failed,
0 skipped. Verdict: `confirmed` current baseline only.

**V1 — Graph verifier and named integration-test behavior**

```bash
rg -n 'GRAPH\.LIST|GRAPH\.QUERY' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
sed -n '53,91p' tests/Hexalith.Memories.IntegrationTests/Tenants/TenantIsolationIntegrationTests.cs
```

Observed: `GraphIsolation` uses `GRAPH.LIST` only. Both named tests provision two tenants and call
verify; neither creates graph content, traverses, searches, nor asserts tenant-distinct payloads.
Verdict: `confirmed` residual.

**V2 — Vector dimension authority**

```bash
rg -n 'rawDimensions|naturalLanguageDimensions|ITenantEmbeddingConfigProvider' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs
sed -n '1,80p' src/Hexalith.Memories.Server/Ingestion/ITenantEmbeddingConfigProvider.cs
```

Observed: raw and NL dimensions are compared only with each other. The tenant provider exists and is
registered, but the verifier does not depend on it. Verdict: `confirmed` residual.

**V3 — Semantic key-family overlap and staging marker shape**

```bash
rg -n 'SemanticKeyPrefixSuffix|LegacyNaturalLanguageSemanticKeyPrefixSuffix|NaturalLanguageSemanticKeyPrefixSuffix|StagingSemanticKeyPrefixSuffixPrefix|StagingNaturalLanguageSemanticKeyPrefixSuffixPrefix' src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs
sed -n '395,443p' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
sed -n '600,678p' src/Hexalith.Memories.Server/Migration/RedisEmbeddingMigrationStore.cs
rg -n 'ValidateMemoryUnitId|ThrowIfNullOrWhiteSpace' src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs
```

Observed: broad active prefixes contain staging and legacy families; the verifier appends `*`; the
two staging hash entry lists omit `tenantId`; and memory-unit validation does not reserve colon-based
namespace tokens. Verdict: `confirmed` residual and prefix-only classification is unsafe.

**V4 — Marker and remediation semantics**

```bash
rg -n 'missing tenantId field|remove mismatched target-prefix hashes' src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
sed -n '1,90p' src/Hexalith.Memories.Contracts/V1/TenantIsolationCheckResult.cs
```

Observed: missing markers join the mismatch list; semantic and syntactic failures use broad removal
guidance; V1 exposes only `Details` and `Remediation` for this classification. Verdict: `confirmed`
residual.

**V5 — Held Story 24.6-24.9 pre-mutation registration state**

```bash
rg -n '24\.[6-9]|24-[6-9]-' _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03-implementation-readiness-remediation-batch.md
```

Observed before the approved mutation: the identities appeared only in the approved 2026-08-03
proposal, which explicitly said they were proposed and unregistered. Verdict: `confirmed`; re-key
before registration was safe and is now explicit in canonical planning.

**V6 — Approved canonical transaction**

```bash
for story in 24-6-graph-content-level-tenant-isolation-evidence 24-7-tenant-configured-vector-dimension-verification 24-8-semantic-isolation-key-family-classification 24-9-non-destructive-tenant-marker-diagnostics; do
  python3 tools/check-story-slice-scope.py --require-record --story-key "$story"
  python3 tools/check-tenant-isolation-evidence.py --story-key "$story" --changed-file src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs
done
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.ReadOpenDeferredBaselines_RealRepo_DoesNotThrowAndReturnsOnlyTargetedEntries -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.DeferredWorkRegister_RealRepo_DeclaresEachIdExactlyOnce
git diff --check
```

Observed 2026-08-04: all four story-scope records passed; all four tenant-evidence records passed
against the representative verifier surface; sprint YAML parsed with Epic 24 `in-progress`, four
ordered backlog rows, and the retrospective action `done`; deferred-ledger parser tests passed 2/2;
tracked and untracked Markdown diff checks reported no whitespace errors (only expected LF-to-CRLF
normalization warnings). The focused V0 build and verifier suite were rerun after registration with
0 warnings/errors and 16/16 tests passing. Verdict: `confirmed` approved planning transaction; the
four implementation stories intentionally remain `backlog` with their declared test results pending.
