---
baseline_commit: 462d37c14f7599d3d66f4fc8a38d9d8fb719f0e4
---

# Story 19.2: Downstream Contract Artifact Generation Decisions

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a downstream integration maintainer,
I want explicit decisions for generated deployment and route artifacts,
so that consumers know whether to rely on maintained docs, generated manifests, or generated OpenAPI/Swagger output.

## Acceptance Criteria

1. Given `MEM-2-ASPIRATE` is carried forward without a story id, when this story runs, then aspirate or equivalent manifest emission is either scheduled for implementation, explicitly accepted as not needed, or deferred with an owner, trigger, and target artifact.

2. Given `MEM-3-OPENAPI` is carried forward without a story id, when this story runs, then OpenAPI/Swagger generation is either scheduled for implementation, explicitly accepted as not needed, or deferred with an owner, trigger, and target artifact.

3. Given maintained docs already exist for deployment configuration and route surface, when generated artifacts remain deferred, then the rationale states why the maintained-doc plus drift-guard tests remain sufficient for current consumers.

## Tasks / Subtasks

- [x] Task 1 — Establish the decision baseline before editing (read-only) (AC: 1, 2, 3)
  - [x] Read the two structured entries this story owns in `_bmad-output/implementation-artifacts/deferred-work.md`: `MEM-2-ASPIRATE` (currently ~line 1496) and `MEM-3-OPENAPI` (currently ~line 1508). Confirm both are `Status: carried-forward` with the open-ended "future … story - unassigned" target. (Verify line numbers — the file shifts between runs; match on the `ID:` token, not the line.)
  - [x] Read the two rollup sections that route these IDs here: `## Deferred Register Backlog Home Rollup (2026-06-30)` (line ~52, "`MEM-2-ASPIRATE` and `MEM-3-OPENAPI` target Story 19.2 unless the story explicitly accepts or reassigns them") and `## Story 19.1 Classification Sweep (2026-06-30)` (line ~66; its table records both as "scheduled story → Story 19.2 — story-id home for Action Item 4", and its anti-over-promotion note states "Stories 19.2/19.3/19.4 make the final implement/accept/defer call for the IDs they own").
  - [x] Read `_bmad-output/planning-artifacts/epics.md` Story 19.2 (lines ~3760-3778) and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30.md` Story 19.2 section (lines ~163-181) plus success criteria (line ~304).
  - [x] Confirm the maintained-doc + drift-guard evidence that grounds AC3 (all four exist today — do NOT modify them): `docs/operations/deployment-configuration.md` (guarded by `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs`) and `docs/operations/route-surface.md` (guarded by `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs`). Each doc already has a "Deferred" section (deployment doc §"Deferred: aspirate manifest emission"; route doc §"Deferred: OpenAPI document generation").
  - [x] Confirm the "no current consumer" facts behind the accept decision: no aspirate/Aspir8 tooling exists in `src/**` or `tools/**` (the only repo mention is the deployment doc itself), and no OpenAPI/Swagger setup exists (`AddOpenApi`/`MapOpenApi`/`Swashbuckle`/`AddSwaggerGen` are all absent from `src/**`). Record these as the evidence for "accept as not needed".
  - [x] Build the disposition decision in working notes before editing any file. Do NOT bulk-rewrite historical prose or touch entries other than `MEM-2-ASPIRATE` and `MEM-3-OPENAPI`.

- [x] Task 2 — Make and record the `MEM-2-ASPIRATE` decision in place (AC: 1, 3)
  - [x] Apply the **recommended disposition: accept-as-not-needed** (`Status: carried-forward` → `accepted`). Edit the `MEM-2-ASPIRATE` block in place, keeping all six required fields valid (see Dev Notes → Recommended field blocks for the exact text). Refresh `Target artifact` to name the real maintained doc + drift-guard test (replacing "future aspirate manifest-emission story - unassigned"), keep the existing `Re-open trigger` (ready-to-apply manifests needed), and put the AC3 sufficiency rationale **and an owner** in prose inside the `Rationale:` line (the schema has no `Owner:` field — see Parser guardrail).
  - [x] The `Rationale:` MUST state why the maintained `deployment-configuration.md` contract plus its `DeploymentConfigurationContractTests` drift guard remain sufficient for current consumers, and that no consumer needs emitted manifests today (AC3). Reference the 2026-05-27 "document now, defer aspirate" locked decision. Keep `Source story: 18-2-deployment-configuration-contract-publication` (the story that *produced* the entry); record the 19.2 decision in the `Rationale` prose, not by rewriting `Source story`.

- [x] Task 3 — Make and record the `MEM-3-OPENAPI` decision in place (AC: 2, 3)
  - [x] Apply the **recommended disposition: accept-as-not-needed** (`Status: carried-forward` → `accepted`). Edit the `MEM-3-OPENAPI` block in place (see Dev Notes → Recommended field blocks). Refresh `Target artifact` to name `docs/operations/route-surface.md` + `RouteSurfaceContractTests`, keep the existing `Re-open trigger` (a consumer needs a generated OpenAPI/Swagger document for client/ACL generation), and put the AC3 sufficiency rationale + owner in the `Rationale:` line.
  - [x] The `Rationale:` MUST state why the maintained `route-surface.md` contract (full 46-route ACL-verifiable surface) plus its `RouteSurfaceContractTests` drift guard remain sufficient, cite Story 18.3 AC2 ("an OpenAPI document **OR** a maintained route-surface doc"), and note the repo has no `AddOpenApi`/`MapOpenApi`/Swashbuckle today and no consumer needs a generated schema (AC3). Keep `Source story: 18-3-invocable-route-and-operation-surface-publication`.

- [x] Task 4 — Add the Story 19.2 decision rollup section (AC: 1, 2, 3)
  - [x] Add a new `## Story 19.2 Downstream Contract Artifact Decisions (2026-06-30)` section to `deferred-work.md`, placed **immediately after the `## Story 19.1 Classification Sweep (2026-06-30)` section and its `### New accepted-debt entries` subsection (ends at the `18.4-TOKEN-EDGE` block, currently ~line 132) and before `## Story 15.5 Triage Rollup (2026-05-15)` (currently ~line 134)**. Do not delete, reorder, or rewrite any existing section — including Story 19.1's classification table.
  - [x] In the new section, record the final decision for both IDs (accept-as-not-needed), note that Story 19.1 routed these here as the "story-id home" and that this story now makes the final implement/accept/defer call per the rollup's "unless the story explicitly accepts or reassigns them" clause, and summarize the AC3 sufficiency rationale (maintained doc + drift-guard test per ID).
  - [x] State explicitly that Epic 18 retrospective **Action Item 4 is already `done`** (closed by Story 19.1 on 2026-06-30 because both IDs already had a story-id home) — this story's accept decision does not change its done-condition, so **no `sprint-status.yaml` `action_items:` edit is required**.

- [x] Task 5 — Validate the governance lane (AC: 1, 2, 3)
  - [x] Run the deferred-work parser regression guard if the SDK + time budget permit (load-bearing): build, then `DiffEngine_Disabled=true dotnet exec <built Hexalith.Memories.Cli.Tests dll>` filtered to `CiTestInventoryTests` (see Dev Notes → Running tests). Flipping both blocks `carried-forward` → `accepted` keeps them valid (`accepted` still requires `Rationale:`, which both retain). If the SDK lane is unavailable, record the limitation and at minimum run `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md` (CR-as-trailing-whitespace warnings on CRLF lines are the expected, documented repo behavior — see deferred entry `15.5-RV1`).
  - [x] Manually verify both edited blocks contain exactly one `ID`, `Status` (now `accepted`), `Source story`, `Target artifact`, `Re-open trigger`, and exactly one `Rationale:` (and zero `Evidence:`), with `Status` drawn from the closed lowercase vocabulary `{open, resolved, accepted, carried-forward}`.
  - [x] Confirm there are NO production source changes (`src/**`), NO test changes, NO `docs/**` changes, and NO submodule-pointer changes in the diff (this is a planning/governance decision sweep). Confirm `deferred-work.md` and this story file use **CRLF**; do not touch `sprint-status.yaml` line endings beyond the create-story status-row update.
  - [x] Record the two final dispositions, the Action Item 4 no-change decision, and any validation limits in this story's Dev Agent Record and Change Log.

## Dev Notes

This is a **planning/governance decision sweep**, not a code change. It is the same lane as Story 19.1 (its immediate predecessor). It edits only `deferred-work.md` and this story file (and `sprint-status.yaml` is updated by the create-story / dev-story status transitions only). It does NOT add aspirate or OpenAPI tooling, does NOT touch `src/**`, `tests/**`, `docs/**`, release tooling, or submodule pointers. The deliverable is two recorded disposition decisions plus a dated rollup. [Source: planning-artifacts/sprint-change-proposal-2026-06-30.md lines 93, 298-299 ("implement Story 19.1 first, then use its classification to decide whether Stories 19.2 through 19.4 need implementation, accepted-debt records, or smaller split stories"); epics.md Story 19.2 ACs]

### What this story decides (the two owned entries)

This story makes the final implement/accept/defer call for the two IDs that Story 19.1 routed here and that Epic 18 retrospective Action Item 4 named. Both are currently `carried-forward` with an open-ended, unassigned "future story" target — exactly the limbo Epic 19 exists to resolve.

| ID | Current state | Maintained-doc home (AC3 evidence) | Drift guard (AC3 evidence) | Recommended decision |
|---|---|---|---|---|
| `MEM-2-ASPIRATE` (aspirate / Kubernetes-Dapr manifest emission, deferred by Story 18.2) | `carried-forward`, target = "future aspirate manifest-emission story - unassigned" | `docs/operations/deployment-configuration.md` (env / Dapr ports / OTLP / pub-sub intake surface) | `tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs` (bidirectional doc↔code tie) | **Accept as not needed** (accept-until-trigger) |
| `MEM-3-OPENAPI` (OpenAPI/Swagger document generation, deferred by Story 18.3) | `carried-forward`, target = "future OpenAPI document-generation story - unassigned" | `docs/operations/route-surface.md` (full 46-route ACL-verifiable surface) | `tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs` (code→doc route tie, count tie) | **Accept as not needed** (accept-until-trigger) |

### Why "accept as not needed", not "schedule" and not "leave carried-forward"

AC1/AC2 each offer three valid dispositions: **schedule**, **accept as not needed**, or **defer with owner/trigger/target**. The recommended call is **accept-as-not-needed** for both, because:

- **No current consumer needs the generated artifact.** Verified: no aspirate/Aspir8 tooling exists anywhere in `src/**` or `tools/**` (only the deployment doc mentions the word), and the repo has no OpenAPI/Swagger setup at all (`AddOpenApi`/`MapOpenApi`/`AddSwaggerGen`/Swashbuckle absent). Nothing today is blocked on either artifact.
- **The maintained-doc + drift-guard pair already covers the consumer need (AC3).** Both contracts are published in operator/ACL-facing form and are tied to code by a build-failing drift test, so a consumer can fill kustomization placeholders and verify a Dapr ACL today without generated output. Story 18.3 AC2 explicitly accepted "an OpenAPI document **OR** a maintained route-surface doc" — the maintained doc *is* the contract, not a stopgap.
- **"Accept" removes the limbo; "leave carried-forward" perpetuates it.** The whole point of Story 19.2 is to convert an open-ended `carried-forward`-without-a-story-id into a bounded decision. `accepted` (per the schema: "the risk remains but is intentionally accepted with a written rationale") with a concrete `Re-open trigger` and a named owner is the strongest resolution and mirrors exactly what Story 19.1 did when it converted the three parked retro-prose items into `accepted` infra-lane entries (`18.4-REDIS-RACE`, `18.8-DAPR-SMOKE`, `18.4-TOKEN-EDGE`).
- **Do not schedule.** Standing up aspirate manifest emission or OpenAPI generation for 46 minimal-API endpoints + the pub/sub controller is a larger, separable platform effort with no pulling consumer; promoting it now would violate the Epic 19 anti-over-promotion guardrail ("the main risk is over-promoting low-value accepted risks into mandatory implementation"). [Source: sprint-change-proposal-2026-06-30.md lines 95, 255; deferred-work.md MEM-2-ASPIRATE / MEM-3-OPENAPI rationales; epics.md Story 18.2/18.3 ACs]

If the dev disagrees with "accept" and prefers to keep `carried-forward`, that is still AC-valid **only if** the entry is enriched to satisfy AC1/AC2 literally — it must carry an explicit **owner** (in `Rationale` prose), a `Re-open trigger`, and a concrete `Target artifact`. The current open-ended "unassigned" target does not meet that bar, so the block must change either way. The accept decision is recommended because it is cleaner and ends the limbo.

### Recommended field blocks (apply verbatim, then adjust prose if you change the decision)

`MEM-2-ASPIRATE` (replace the existing block in place):

```
- ID: MEM-2-ASPIRATE
  - Status: accepted
  - Source story: 18-2-deployment-configuration-contract-publication
  - Target artifact: docs/operations/deployment-configuration.md (maintained deploy-config contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs; a future aspirate/Aspir8 manifest-emission story stays unassigned until the re-open trigger fires.
  - Re-open trigger: a downstream consumer needs ready-to-apply Kubernetes/Dapr manifests emitted from the AppHost topology rather than a hand-filled documented contract.
  - Rationale: Story 19.2 (2026-06-30) accepts the documented-contract approach as sufficient for current consumers and declines to schedule aspirate emission. The maintained deploy-config contract publishes every env/port/OTLP/pub-sub literal an operator must supply, and DeploymentConfigurationContractTests fails the build on doc<->code drift, so consumers fill kustomization placeholders today without generated manifests; no current consumer requires emitted manifests, and no aspirate tooling exists in src/** or tools/**. Per the 2026-05-27 "document now, defer aspirate" locked decision this is accept-until-trigger. Owner: AppHost / release maintainer.
```

`MEM-3-OPENAPI` (replace the existing block in place):

```
- ID: MEM-3-OPENAPI
  - Status: accepted
  - Source story: 18-3-invocable-route-and-operation-surface-publication
  - Target artifact: docs/operations/route-surface.md (maintained route/operation-surface contract) guarded by tests/Hexalith.Memories.Server.Tests/Deployment/RouteSurfaceContractTests.cs; a future OpenAPI/Swagger document-generation story stays unassigned until the re-open trigger fires.
  - Re-open trigger: a downstream consumer needs a generated OpenAPI/Swagger document (machine-consumable schema for client/ACL generation) rather than the maintained route-surface contract.
  - Rationale: Story 19.2 (2026-06-30) accepts the maintained route-surface contract as sufficient for current consumers and declines to schedule OpenAPI/Swagger generation. Story 18.3 AC2 explicitly permitted "an OpenAPI document OR a maintained route-surface doc"; route-surface.md publishes the full 46-route ACL-verifiable surface and RouteSurfaceContractTests ties it to Program.cs so it cannot drift. The repo has no AddOpenApi/MapOpenApi/Swashbuckle today and no consumer needs a generated schema; standing up OpenAPI for 46 minimal-API endpoints plus the pub/sub controller is accept-until-trigger. Owner: Server / API maintainer.
```

Note: keep the human-readable one-line summary bullet that precedes each field block if the existing entry has one; only the field block changes. Match the existing indentation exactly (two-space indent on the field sub-bullets) so the parser's anchored regex still matches.

### Recommended rollup section text (Task 4)

Insert after the Story 19.1 section, before the Story 15.5 section:

```
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
```

### Parser guardrail (load-bearing regression risk)

`tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` parses `deferred-work.md` structured entries and **fails loudly** (`ShouldAssertException`) on malformed blocks. Every edited active entry MUST satisfy:

- `AllowedDeferredStatuses = ["open", "resolved", "accepted", "carried-forward"]` — closed, lowercase. `accepted` is valid; synonyms (`done`, `closed`, `not-needed`, `declined`) fail. [CiTestInventoryTests.cs:25]
- Required fields present: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`. A missing required field fails loudly. [CiTestInventoryTests.cs:365-374]
- `accepted` requires `Rationale:` (NOT `Evidence:`). Both edited blocks keep their `Rationale:` line and must not gain an `Evidence:` line. [CiTestInventoryTests.cs:376-431]
- The `ID:` value is matched as a verbatim whitespace-free token (`StructuredIdShape`: alphanumeric, dot, dash). `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` are unchanged — keep them exactly, as they are referenced from the rollups and from `epics.md`/the 19.1 story. [deferred-work.md#Schema lines 12-14]
- The schema has NO `Owner:` field — an `Owner:` line is silently ignored by the parser. Record the owner in prose inside the `Rationale:` line (as the recommended blocks do), not as a structured field. [CiTestInventoryTests.cs StructuredFieldRegex]
- These two entries are NOT release-lane baselines (`S11-F*` family), so do NOT add a `Test:` field. [deferred-work.md#Schema "Optional fields"]

Field block format (anchored at the start of each indented sub-bullet):

```
- ID: <verbatim id>
  - Status: <open|resolved|accepted|carried-forward>
  - Source story: <story/retro/review key>
  - Target artifact: <repo-relative path or planning artifact>
  - Re-open trigger: <one sentence>
  - Rationale: <required for accepted/carried-forward>   # Evidence: only for resolved
```

[Source: deferred-work.md#Schema-for-Active-Entries lines 3-50; 19-1 dev notes Parser guardrail; CiTestInventoryTests.cs]

### Pattern precedents (reuse, do not reinvent)

- **Story 19.1** (immediate predecessor, `_bmad-output/implementation-artifacts/19-1-deferred-register-active-entry-classification-sweep.md`, status `review`) is the closest analog and the direct upstream: it classified all 30 active entries, routed `MEM-2-ASPIRATE`/`MEM-3-OPENAPI` to this story, and converted three retro-prose items into `accepted` infra-lane entries with re-open triggers. Mirror its rollup-heading + per-entry-edit + "preserve historical prose, no submodule churn" discipline. Its dev record documents the exact parser-guard run (`CiTestInventoryTests` 48/48) and the CRLF/LF hygiene approach. [Source: 19-1-...md Dev Agent Record + Parser guardrail]
- **Story 15.5** (`15-5-deferred-register-triage-sweep.md`) is the original bounded-triage precedent that added a dated rollup section, applied structured dispositions including `accepted` (see `12.4-RV10`, `12.4-RV11`, `13.2-RV4`), and validated with `CiTestInventoryTests`. Its `accepted` blocks are worked examples of the exact shape this story applies. [Source: deferred-work.md lines 178-198, 251-259]
- **Story 14.5** introduced the structured-field schema and the structured-field parser. [Source: 14-5-deferred-register-governance-and-sprint-status-hygiene.md]

### Scope boundaries — what this story must NOT do

- Do NOT implement aspirate manifest emission or OpenAPI/Swagger generation. This story records a *decision*, not an implementation. [Source: epics.md Story 19.2 ACs — "scheduled … or explicitly accepted … or deferred"]
- Do NOT edit any deferred-work entry other than `MEM-2-ASPIRATE` and `MEM-3-OPENAPI`; do NOT rewrite Story 19.1's classification table or any historical prose/rollup. [Source: Epic 19 Preflight "Do not bulk-rewrite historical deferred prose"; sprint-change-proposal-2026-06-30.md line 137]
- Do NOT modify the maintained docs (`deployment-configuration.md`, `route-surface.md`) or their drift-guard tests. Their existing "Deferred …" sections already say "no follow-up story id is assigned yet", which remains literally true under an **accept** decision (we are accepting, not scheduling an implementation story) — so no doc edit is needed and editing them risks tripping the two drift-guard tests. If a future maintainer wants to refresh those "Deferred" sections to cite the accept decision, that is a separate doc task that must re-run `DeploymentConfigurationContractTests` + `RouteSurfaceContractTests`. [Source: docs/operations/deployment-configuration.md §"Deferred: aspirate manifest emission"; docs/operations/route-surface.md §"Deferred: OpenAPI document generation"]
- Do NOT change `sprint-status.yaml` `action_items:`. Epic 18 Action Item 4 was set `done` by Story 19.1; an accept decision does not alter its done-condition. Only update an action item when its acceptance condition newly becomes met (none do here). [Source: epics.md Story 19.1 AC4; sprint-status.yaml action_items Epic-18 item ("status: done … all five have homes")]
- Do NOT reopen `epic-15` or `epic-18` (both `done`); only reference the completed source stories (18.2/18.3). [Source: epics.md Story 19.1 AC3 pattern; sprint-change-proposal-2026-06-30.md]

### Latest tech context (informs the re-open triggers only — no implementation here)

These notes exist so the re-open triggers point at real tooling if a future story schedules implementation. They are NOT a signal to implement now.

- **aspirate** = the community **Aspir8** tool (`aspirate` CLI) that generates ready-to-apply Kubernetes manifests (and Dapr component manifests) from an Aspire AppHost manifest. It is the natural "equivalent manifest emission" target for `MEM-2-ASPIRATE` if a consumer ever needs generated overlays instead of the hand-filled documented contract.
- **OpenAPI on .NET 10** = the built-in `Microsoft.AspNetCore.OpenApi` package (`builder.Services.AddOpenApi()` + `app.MapOpenApi()`) is the modern, Swashbuckle-free path to emit an `openapi.json` for minimal-API endpoints; Swashbuckle/`AddSwaggerGen` is the alternative. Either would satisfy `MEM-3-OPENAPI` if scheduled. The repo currently has none of these. [Source: route-surface.md §"Deferred: OpenAPI document generation" — confirms `Microsoft.AspNetCore.OpenApi` is absent today]
- No version research is required for this decision story because no package is added and no code is written. [Source: project-context.md — central package management; this story adds no `PackageReference`]

### Testing standards

- Tests are xUnit v3 + Shouldly. The only test relevant to this governance lane is `CiTestInventoryTests` (the `deferred-work.md` structured-entry parser). No new tests are required for a decision sweep, but the parser must stay green after the two `Status` edits. [Source: project-context.md Testing Rules; 19-1-...md Testing standards]
- Running tests in this sandbox: plain `dotnet test` fails (VSTest `SocketException (13)`). Build, then `DiffEngine_Disabled=true dotnet exec` the built `Hexalith.Memories.Cli.Tests` dll, filtering to `CiTestInventoryTests` (e.g. `-class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`). This is the Epic 17/18/19.1 carry-forward workaround. [Source: project memory "Running .NET tests in this sandbox"; 19-1-...md Debug Log]

### Line endings (avoid whole-file churn — Epic 18 Action Item 1)

- This story file and `deferred-work.md` are **CRLF** (`.editorconfig` `end_of_line = crlf`). Keep them CRLF; an editor that rewrites them to LF produces a whole-file diff and a review finding. Edit only the two field blocks + the new rollup section; leave the rest of `deferred-work.md` byte-for-byte.
- `sprint-status.yaml` is currently **LF** in the working tree — the create-story / dev-story status-row transition must preserve LF; do not flip it to CRLF and do not touch it beyond the `19-2-...` status row.
- [Source: .editorconfig lines 3-9; `file` on the targets; project memory "CRLF line endings required"; 19-1-...md Line endings note]

### Project Structure Notes

- Edited files live under `_bmad-output/implementation-artifacts/`: `deferred-work.md` (two field-block edits + one new rollup section) and this story file. `sprint-status.yaml` is updated only by the status transitions (create-story: `19-2-...` `backlog` → `ready-for-dev`; dev-story: `ready-for-dev` → `in-progress` → `review`).
- No conflicts with the unified project structure: this story edits Markdown governance artifacts only; it introduces no new code paths, namespaces, packages, routes, or tests.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic-19 / Story-19.2 (lines 3760-3778) — story statement and the three ACs]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30.md — Story 19.2 section (lines 163-181); developer-agent sequencing (lines 298-299); success criteria (line 304)]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#MEM-2-ASPIRATE (lines ~1496-1501) and #MEM-3-OPENAPI (lines ~1508-1513); #Deferred-Register-Backlog-Home-Rollup-2026-06-30 (lines 52-64); #Story-19.1-Classification-Sweep-2026-06-30 (lines 66-132); #Schema-for-Active-Entries (lines 3-50)]
- [Source: _bmad-output/implementation-artifacts/19-1-deferred-register-active-entry-classification-sweep.md — predecessor pattern, Parser guardrail, CRLF/LF hygiene, Action Item 4 closure]
- [Source: docs/operations/deployment-configuration.md (maintained deploy-config contract; §"Deferred: aspirate manifest emission") and docs/operations/route-surface.md (maintained route surface; §"Deferred: OpenAPI document generation") — AC3 sufficiency evidence]
- [Source: tests/Hexalith.Memories.Server.Tests/Deployment/DeploymentConfigurationContractTests.cs and RouteSurfaceContractTests.cs — drift guards that make the maintained docs authoritative]
- [Source: tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs — deferred-work parser contract]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#action_items (Epic 18 item, status done) and #STATUS-DEFINITIONS]
- [Source: _bmad-output/project-context.md — repo-wide rules (CRLF, central package management, submodule discipline, no production-code change here)]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-30: Loaded BMAD dev-story workflow customization (`activation_steps_prepend` / `append` empty), project config, project-context files, story file, and sprint status.
- 2026-06-30: Verified Task 1 evidence: `MEM-2-ASPIRATE` / `MEM-3-OPENAPI` routing, Story 19.2 planning references, maintained-doc drift guards, and no aspirate/OpenAPI tooling in `src/**` or `tools/**`.
- 2026-06-30: Confirmed `deferred-work.md` contains the Story 19.2 accept-as-not-needed decisions for both owned IDs and the Story 19.2 rollup section.
- 2026-06-30: `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj -v:n` failed during restore/build startup with no MSBuild errors or warnings emitted.
- 2026-06-30: `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests` passed: 48 total, 0 failed.
- 2026-06-30: Broader existing CLI test assembly run reached 415 tests; 2 failed due sandbox socket permission (`QuickstartPrerequisiteTests` TCP listener setup), unrelated to this Markdown governance sweep.
- 2026-06-30: `git diff --check -- _bmad-output/implementation-artifacts/deferred-work.md` reports CRLF-as-trailing-whitespace warnings on added Markdown lines; this is the documented expected behavior for CRLF deferred-work edits.
- 2026-06-30: Story-scope diff check found no `src/**`, `tests/**`, or `docs/**` changes. The working tree already contains unrelated dirty files and a `references/Hexalith.Tenants` submodule pointer change, left untouched.

### Completion Notes List

- Final disposition recorded for `MEM-2-ASPIRATE`: `accepted` / not needed now, with maintained `docs/operations/deployment-configuration.md` plus `DeploymentConfigurationContractTests` as the current consumer contract and drift guard.
- Final disposition recorded for `MEM-3-OPENAPI`: `accepted` / not needed now, with maintained `docs/operations/route-surface.md` plus `RouteSurfaceContractTests` as the current consumer contract and drift guard.
- Added/verified Story 19.2 rollup documenting that Story 19.1 routed both IDs here and that Epic 18 Action Item 4 remains `done`; no `action_items:` edit was required.
- Validation passed for the load-bearing deferred-work parser guard (`CiTestInventoryTests` 48/48). Full rebuild was blocked by an undiagnosed MSBuild startup failure; broader existing CLI assembly run was blocked only by sandbox TCP listener permission.

### File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/19-2-downstream-contract-artifact-generation-decisions.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

| Date | Phase | Summary |
|---|---|---|
| 2026-06-30 | create-story | Story drafted: decision sweep for `MEM-2-ASPIRATE` / `MEM-3-OPENAPI`. Recommended disposition = accept-as-not-needed for both, grounded in existing maintained docs + drift-guard tests (AC3) and verified absence of any aspirate/OpenAPI tooling or pulling consumer. No production/test/doc/submodule scope. Status → ready-for-dev. |
| 2026-06-30 | dev-story | Accepted `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` as not needed now, verified the Story 19.2 rollup and Action Item 4 no-change decision, ran the deferred-work parser guard successfully, and moved status to review. |
