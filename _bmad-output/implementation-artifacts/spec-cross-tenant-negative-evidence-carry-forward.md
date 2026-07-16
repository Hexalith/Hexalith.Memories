---
title: 'Add a fail-closed project-context bridge for bmad-spec'
type: 'bugfix'
created: '2026-07-16'
status: 'done'
review_loop_iteration: 3
baseline_commit: 'c46f734f3a8eaeb6ccd229bb819db23f62f8563d'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-cross-tenant-negative-evidence-refresh.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved repository-lifetime cross-tenant negative-evidence guard is present in `epics.md`, `_bmad-output/project-context.md`, and sprint status. `bmad-spec` references a nonexistent root `project-context.md`; a team-only customization can repair normal resolution but disappears when resolver execution fails and the skill falls back to generated defaults, so future specs can still miss the guard.

**Approach:** Add a root `project-context.md` forwarding bridge to the canonical `_bmad-output/project-context.md`, making both normal and resolver-fallback activation safe without editing generated files or duplicating policy content. Cover the resolved default, bridge contract, canonical guard payload, and fallback consumption contract in the existing tooling test lane.

## Boundaries & Constraints

**Always:** Keep `_bmad-output/project-context.md` canonical and make the root file a forwarding bridge only; preserve the existing repository-lifetime rule, its Story 20.2 denial-before-dependency anchor, its Story 24.3 verifier/tenant-marker anchor, and the ongoing `in-progress` action; prove the generated default resolves to the readable bridge, the bridge directs agents to the canonical file, the canonical guard payload remains intact, and the documented fallback still consumes persistent facts.

**Ask First:** Any proposal to replace the shared `bmad-spec` default, move or duplicate the canonical project-context content, add a general evidence-attachment validator, or change the action from `in-progress` to `done`.

**Never:** Edit generated `.agents/skills/bmad-spec` files; copy the canonical policy body into the root bridge; rewrite the approved sprint-change proposal or completed stories; claim that historical integration evidence passed where Story 24.3 records it as blocked; touch product runtime code, tenant routing, authentication, storage, submodules, or unrelated customizations.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Normal resolution | Generated `bmad-spec` defaults resolve successfully | Root persistent fact names an existing bridge that directs agents to canonical project context | Unit test fails if the fact, bridge, or canonical target disappears |
| Resolver fallback | Customization resolution fails and `bmad-spec` reads generated defaults | The same root bridge still directs activation to canonical project context | Contract test fails if fallback or persistent-fact consumption is removed |
| Canonical policy drift | Bridge and canonical file exist but the attached-evidence rule or anchors are removed | CI rejects the change before future specs lose the guard | Payload assertion fails on the stable rule and Story 20.2/24.3 anchors |

</frozen-after-approval>

## Code Map

- `project-context.md` -- stable root bridge consumed by the generated normal and fallback fact path; contains no duplicated implementation policy.
- `_bmad-output/project-context.md` -- canonical attached-negative-evidence rule and Story 20.2/24.3 anchors.
- `.agents/skills/bmad-spec/{customize.toml,SKILL.md}` and `.claude/skills/bmad-spec/{customize.toml,SKILL.md}` -- generated defaults and activation/fallback contracts that name and consume the root fact; evidence sources, never edit.
- `.agents/skills/bmad-generate-project-context/SKILL.md` and `.claude/skills/bmad-generate-project-context/SKILL.md` -- generated manual-merge fallback and append-step execution contracts; evidence sources, never edit.
- `_bmad/custom/bmad-generate-project-context.toml` -- team-owned update/write selection directive protecting the root bridge.
- `tests/tooling/bmad_customization/bmad_customization_test.py` -- existing resolver/contract fixture lane, including concurrent team customizations that must be preserved.
- `.github/workflows/ci.yml` -- already runs the BMad customization unittest lane.

## Tasks & Acceptance

**Execution:**
- [x] `project-context.md` -- replace the bridge with an exact repository-root-relative forwarding/control contract that identifies `_bmad-output/project-context.md` as this repository's only project-context policy source, contains no implementation policy, halts for a missing, unreadable, empty, or marker-invalid canonical file, and forbids generators from updating the bridge -- make normal, fallback, and broad-glob consumers fail closed without overstating the bridge's authority over other mandatory policies.
- [x] `_bmad/custom/bmad-generate-project-context.toml` -- replace the `PROJECT_CONTEXT_BRIDGE:` activation directive with update/write-only selection of `_bmad-output/project-context.md`, explicitly treating other discovered project-context files as read-only -- protect the root bridge without falsely claiming that generator discovery reads only the canonical file.
- [x] `tests/tooling/bmad_customization/bmad_customization_test.py` -- add focused static contract coverage for the exact bridge; the full normalized canonical attached-evidence rule; the full operative epics carry-forward guard; the unique ongoing sprint action; exact normal/fallback root facts on both `.agents` and `.claude`; and generator resolver output, manual team-merge fallback, append execution, and exact activation directive on both generated surfaces -- fail closed on delivery, payload, governance, or writer-selection drift while preserving concurrent test work.
- [x] `_bmad-output/implementation-artifacts/spec-cross-tenant-negative-evidence-carry-forward.md` -- attach matrix coverage, current-baseline inventory/checks, implementation and owned-patch digests, and reviewed exclusions while leaving the ongoing action `in-progress` -- make completion auditable.

**Acceptance Criteria:**
- Given generated customization resolves normally, when its persistent facts are inspected, then they are a list containing exactly one `file:{project-root}/project-context.md`, whose readable bridge directs agents to canonical project context.
- Given resolver execution fails, when `bmad-spec` follows its documented generated-default fallback, then the fallback still consumes the same root file fact and reaches the canonical guard through the bridge.
- Given the bridge remains but the canonical attached-negative-evidence rule or Story 20.2/24.3 anchors drift, when the customization fixture runs, then it fails.
- Given the bridge is inspected, when its content is compared with the approved forwarding contract, then it matches exactly, contains no implementation policy, resolves paths from the repository root, and requires halt/report behavior when canonical context is missing, unreadable, empty, or missing the stable attached-evidence marker.
- Given workflows load `**/project-context.md`, when they encounter both bridge and canonical context, then the bridge remains a forwarding/control fact and `_bmad-output/project-context.md` remains this repository's only project-context policy source.
- Given `bmad-generate-project-context` discovers both files, when its resolved activation directive runs, then it may read discovered context but selects and updates only `_bmad-output/project-context.md` and never rewrites the root bridge.
- Given either generated agent surface is used and resolver execution fails, when `bmad-generate-project-context` performs its documented base-to-team-to-user manual merge and executes append steps, then the same team-owned update/write directive is applied before the main workflow.
- Given this change is completed, when owned scope is reviewed, then existing epics/project-context guards and the ongoing sprint action remain intact, while generated files, runtime code, submodules, and unrelated concurrent changes remain untouched.

## Spec Change Log

- 2026-07-16, review loop 1: Parallel review found the success-path team override vanished during `bmad-spec` resolver fallback, leaving scope-sensitive specs fail-open. Human authorization changed the frozen approach to a root forwarding bridge; tasks now test normal resolution, fallback consumption, and canonical payload rather than only file existence. This avoids generated-file edits and policy duplication. KEEP the existing canonical guard, Story 20.2/24.3 anchors, ongoing action status, focused customization lane, and strict unrelated-change boundary.
- 2026-07-16, review loop 2: Review found the root bridge also enters broad `**/project-context.md` consumers and can be selected by `bmad-generate-project-context`, while substring assertions allowed bridge contradictions and canonical-policy weakening. The plan now treats glob consumers as affected, makes the bridge exact and fail-closed, adds a team-owned canonical-writer directive, protects every operative evidence clause and ongoing governance anchor, and uses baseline-relative scope/digest evidence. This avoids a ruleless bridge becoming the update target or a neutered policy passing CI. KEEP the forwarding design, normal/fallback convergence, generated-file boundary, passing customization lane, and review-loop 1 canonical anchors.
- 2026-07-16, review loop 3: Review found that the bridge overstated its authority as the only policy source, mislabeled its own control rules, omitted empty/invalid canonical failure modes, and told the generator to read only the canonical file even though discovery precedes the append directive. Coverage also omitted the `.claude` generated surface and did not pin generator manual fallback/append execution or the complete operative governance rules. The plan now narrows authority to the repository's project-context policy, distinguishes control text from implementation policy, validates the stable marker, makes generator selection update/write-only, and statically verifies normal and manual-fallback contracts across both generated surfaces. This avoids contradictory control language and false guarantees about discovery order. KEEP the frozen bridge approach, canonical guard, Story 20.2/24.3 anchors, ongoing action, generated-file boundary, and baseline-scoped evidence.

## Design Notes

The root bridge is an exact forwarding/control document, not an implementation-policy source. Normal resolution and resolver fallback use it directly; glob-based workflows may load it alongside canonical context, where it identifies and validates the canonical project-context policy source. Project-context generation may read all discovered context, while a refresh-safe activation directive pins only its update/write target to `_bmad-output/project-context.md`. Both committed generated agent surfaces are contract evidence but remain untouched. Story 20.2 and Story 24.3 remain historical runtime anchors and are not falsely reported as newly rerun.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"` -- expected: all customization fixtures pass.
- `uv run _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-spec --key workflow` -- expected: JSON contains exactly one root `project-context.md` persistent fact.
- `uv run _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-generate-project-context --key workflow` -- expected: exactly one `PROJECT_CONTEXT_BRIDGE:` directive selecting the canonical writer target.
- `uv run _bmad/scripts/resolve_customization.py --skill .claude/skills/bmad-spec --key workflow` -- expected: JSON matches the `.agents` root-fact result.
- `uv run _bmad/scripts/resolve_customization.py --skill .claude/skills/bmad-generate-project-context --key workflow` -- expected: JSON matches the `.agents` canonical-writer result.
- `git diff --check c46f734f3a8eaeb6ccd229bb819db23f62f8563d -- project-context.md _bmad/custom/bmad-generate-project-context.toml tests/tooling/bmad_customization/bmad_customization_test.py _bmad-output/implementation-artifacts/spec-cross-tenant-negative-evidence-carry-forward.md` -- expected: no whitespace errors in baseline-relative owned deltas.
- `git diff --name-status c46f734f3a8eaeb6ccd229bb819db23f62f8563d` plus `git ls-files --others --exclude-standard` -- expected: record the four owned paths and list unrelated planning, runtime, submodule, and test exclusions separately.
- `sha256sum project-context.md _bmad/custom/bmad-generate-project-context.toml` and `git diff c46f734f3a8eaeb6ccd229bb819db23f62f8563d -- tests/tooling/bmad_customization/bmad_customization_test.py | sha256sum` -- expected: record implementation-file and isolated owned-patch snapshots.

**Results (2026-07-16, review loop 3):**
- `test_cross_tenant_project_context_delivery_contract` covers all three frozen matrix rows and the loop-3 acceptance boundaries. For normal resolution it resolves both generated surfaces to the single root fact and exact update/write directive. For resolver fallback it pins the complete normalized activation blocks for both generated `bmad-spec` and generator surfaces, including default/fact consumption, base → team → user merge, and ordered append execution. For canonical drift it requires the exact rule in the operative `### Testing Rules` section, the exact guard in the operative audit-remediation phase, the exact bridge, the unique Epic 0 `in-progress` action record, the canonical BMM output folder, and every generated context reference/write site. `test_policy_contract_extractor_rejects_inactive_markdown` proves backtick/tilde fences, HTML comments, indented code, and retired sections cannot spoof active evidence. The full customization lane ran 16 tests with 0 failures.
- Independent resolver runs for both `.agents` and `.claude` returned exactly `["file:{project-root}/project-context.md"]` for `bmad-spec`; both generator surfaces returned exactly one `PROJECT_CONTEXT_BRIDGE:` append directive, allowed matched-context reads, and selected only `_bmad-output/project-context.md` for update/write.
- Baseline-relative owned-path whitespace validation exited 0; Git emitted only repository LF-to-CRLF normalization notices. Owned snapshots are `project-context.md` SHA-256 `ff4d21e717844fd824b9f6f0c23d7f6690e08c3e18e6d56a2d40fb2f94612e48`, `_bmad/custom/bmad-generate-project-context.toml` SHA-256 `6f34121cecbf0bbd2ddbf7f123198acd27e4e1492d57b54ae63b9fb9fa25a248`, and the isolated baseline-relative customization-test patch SHA-256 `3955e262fc850a3e549a33d47d8f32670f601f5d8f71f855ecdb397fe63d8dc8`.
- Owned paths are the root bridge, generator team customization, focused customization-test hunk, and this spec. The final review snapshot excluded these concurrent baseline-relative changes: `deferred-work.md`; Epic 26 remediation evidence, retrospective, closure clarification, and benchmark-gate artifacts; sprint status; architecture and epics; the tracked Epic 26 closure and stale-HXL001 proposals; EventStore and FrontComposer submodules; `FusionWeights.cs`; FusionWeights serialization, FusionEngine, persistence-compatibility, coverage-gate tests, and `tests/README.md`; plus five untracked proposals for access-telemetry retention, contract-doc drift, one-shot tracking, standalone tracking, and Story 19.4 marker consistency. None is part of the story-owned patch.
- Story-owned affected surfaces are `bmad-spec` normal/fallback context delivery, broad-glob bridge consumption, and `bmad-generate-project-context` update/write selection. The story-owned patch changes no tenant route, authorization, storage, query, verifier, submodule, or other product-runtime behavior. Story 20.2 denial-before-dependency and Story 24.3 verifier/tenant-marker evidence therefore remain the applicable canonical historical anchors rather than product tests falsely claimed as rerun; focused negative evidence for this governance change is the inactive/retired-policy perturbation test plus exact drift contracts above.
- Parallel adversarial, edge-case, and verification-gap review found no intent gap or bad-spec defect. Patch findings were closed by scoping validity to the active Testing Rules section, hardening Markdown-state extraction, pinning complete activation and concrete writer paths, relaxing unrelated sprint owner/comment coupling, recording both resolver surfaces, and refreshing this inventory. Dynamic mutation of an LLM-authored workflow and personal override prohibition were rejected as outside the static contract and approved repository-owned scope.

## Suggested Review Order

**Context delivery**

- Forwarding bridge makes canonical context mandatory and fail-closed.
  [`project-context.md:3`](../../project-context.md#L3)

- Writer directive preserves the bridge while allowing context discovery.
  [`bmad-generate-project-context.toml:6`](../../_bmad/custom/bmad-generate-project-context.toml#L6)

**Acceptance and evidence**

- Approved tasks keep canonical policy singular and runtime scope untouched.
  [`spec-cross-tenant-negative-evidence-carry-forward.md:51`](spec-cross-tenant-negative-evidence-carry-forward.md#L51)

- Audit results record matrix coverage, anchors, digests, and concurrent exclusions.
  [`spec-cross-tenant-negative-evidence-carry-forward.md:89`](spec-cross-tenant-negative-evidence-carry-forward.md#L89)

**Regression guard**

- End-to-end contract pins normal, fallback, policy, action, and writer surfaces.
  [`bmad_customization_test.py:247`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L247)

- Markdown-state parser rejects inactive policy placements.
  [`bmad_customization_test.py:189`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L189)

- Negative fixtures prove fenced, commented, indented, and retired rules fail.
  [`bmad_customization_test.py:380`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L380)
