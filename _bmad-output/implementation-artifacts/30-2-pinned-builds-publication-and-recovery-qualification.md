---
title: 'Pinned Builds publication and recovery qualification'
type: 'chore'
created: '2026-10-31'
status: 'done'
workflow_status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '30-2-pinned-builds-publication-and-recovery-qualification'
baseline_commit: 'aa097b52a62b80af32c10267a100646b95be8ac2'
context: ['{project-root}/_bmad-output/implementation-artifacts/epic-30-context.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Later release stories would consume the pinned Builds container-publication contract without proof that it handles Memories' complete ten-package/five-image set, drift, partial failure and recovery.

**Approach:** Add a Memories-owned, offline qualification harness that drives the pinned Builds helpers over the declared release set and writes six revision-bound evidence files plus a pending owner-approval record.

## Boundaries & Constraints

**Always:** Bind every result to Builds `81c88b34d09d34260b92d7f0928dbbc1c7446baa`, its executed helper SHA-256s, the Memories source SHA (with a dirty-tree flag), and the harness and profile SHA-256s. Execute the real pinned helpers; fake only network, registry, docker and dotnet seams. Fail closed on any mismatch, missing member, unsupported phase, ambiguous destination or incomplete evidence. Feed Builds a transient `id`-shaped copy of `tools/release-packages.json`.

**Never:** Push, publish, upload, contact a registry, or change the release/recovery workflows, publishers, manifest schema or `CiTestInventoryTests.cs`. Never copy Builds logic, edit `references/`, synthesize owner approval, or mark 30.2 done without it.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Complete set | Profile = repository set | All three phases plus five validate/smoke runs pass | N/A |
| Set drift | Member dropped, added, renamed or remapped; count ≠ 10 | Refused caller-side, and by Builds between phases | `fail` + reason |
| Partial publication | Third image fails | Members classified published/ambiguous/not-attempted | Ambiguous never success |
| Recovery | Partial evidence | Observe-created re-reads published; missing set exact; rerun refuses collisions | Caller-owned publish handed to 30.5 |
| Bad pin or evidence | Wrong SHA/hash, missing file, unknown phase | Overall `fail`, non-zero exit | No partial pass |

## Approved Decisions (2026-10-31)

The user selected caller-side qualification. Each Builds gap becomes a named caller-owned exception, and results may pass and receive owner approval:

- no dry-run mode → fixture-driven harness
- no partial-result document → derived from per-image evidence
- no missing-only republish → 30.5 recovery
- no caller-set check → declared profile
- `warn` upload with no hash manifest → caller hash manifest, and the caller upload must fail on missing files
- `prepare-domain-release` cannot publish containers → finding for 30.3/30.4

The user also kept the full spec (about 1,900 tokens). Approval grants no publication, predecessor or later-story credit.

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Github/publish-containers/publication_preflight.py` -- `--phase verify|publish|container`; `build_publication_identity`; mockable seams `prove_current_green_source`, `read_external_version_observations`, `destination_probe`; refusal codes `publication-identity-changed`, `frozen-identity-collision`, `preflight-sequence-invalid`, `version-collision`.
- `.../publish-containers.sh` -- `HEXALITH_CONTAINER_PROJECTS` lines `csproj|repo`; env overrides `HEXALITH_OCI_VALIDATOR`, `HEXALITH_CONTAINER_SMOKE`, `HEXALITH_PUBLICATION_PREFLIGHT`, `HEXALITH_CONTAINER_EVIDENCE_DIRECTORY`; stops at the first failure, with no set summary.
- `.../oci_registry_validator.py` -- `validate_capture(root, labels)`; `--created`/`--observe-created`. `smoke_container_platforms.py` uses fake docker/curl.
- `.../tests/test_publish_script_contract.py` -- the pattern for fake bins (reuse the approach, not the code). `references/Hexalith.Builds/.github/workflows/domain-release.yml:552-560` -- reference upload step.
- `src/*/…csproj` (Server, Mcp, AccessTelemetry, AccessTelemetry.Clock, DomainService) -- `EnableContainer` + `ContainerRepository`. `tools/release-packages.json` -- ten `packageId` entries.
- `.github/workflows/recover-partial-release.yml:137-151,183-185` -- still accepts only 2/4 images and 9 packages; record this as a 30.5 finding and do not change it.

## Tasks & Acceptance

**Execution:**
- [x] `tools/builds-publication-qualification.json` -- declare the Builds SHA, the package count of 10, and the five `csproj|repository` mappings -- an independent set declaration.
- [x] `tools/qualify-builds-publication.py` -- argparse harness; verify the pin and helper hashes; run the six scenario groups; write `--output-directory` JSON (`schemaVersion`, `checkpoint`, `result`, bindings, findings, exceptions); exit non-zero on failure.
- [x] `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py` -- unittest coverage of the matrix and every fail-closed branch.
- [x] `.github/workflows/ci.yml`, `tests/tooling/ci_alignment/ci_alignment_test.py` -- add a `test-unit-contract` discover step and its `MANDATORY_COMMANDS` entry.
- [x] `_bmad-output/implementation-artifacts/30-2-qualification/` -- generated evidence; `approval.md` lists the SHAs and evidence hashes and stays `pending`.
- [x] `_bmad-output/planning-artifacts/epics.md`, `sprint-status.yaml` -- update the 30.2 checkpoint states, except approval.

**Acceptance Criteria:**
- Given the harness at the pinned SHA, when run against the repository, then all six evidence files bind identical Builds, source and profile identities.
- Given passing upstream fixtures alone, when qualification evaluates, then the story does not reach done without recorded owner approval.

## Historical Context Classification

| Source | Classification | Permitted use |
| --- | --- | --- |
| Story 30.1 | historical-reference-only | Pin and CI wiring pattern |
| Story 30.3/30.5 | historical-reference-only | Receive findings only |

## Slice Proof

One outcome: a revision-bound qualification verdict. Release migration, publication and recovery changes stay with 30.3–30.5.

## File Scope

Allowed files for this story:

- `tools/builds-publication-qualification.json`
- `tools/qualify-builds-publication.py`
- `tests/tooling/builds_publication_qualification/**`
- `.github/workflows/ci.yml`
- `tests/tooling/ci_alignment/ci_alignment_test.py`
- `_bmad-output/implementation-artifacts/30-2-qualification/**`
- `_bmad-output/implementation-artifacts/30-2-pinned-builds-publication-and-recovery-qualification.md`
- `_bmad-output/implementation-artifacts/epic-30-context.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`

## Cross-Tenant Negative Evidence

Release tooling only; tenant-negative gates are unchanged.

## Implementation Notes

- The pinned preflight, validator and smoke confine every path to the working directory, so the harness installs the five pinned helpers into a temporary workspace exactly as `action.yml` does. The real publisher, preflight, validator and smoke run unmodified. Only `prove_current_green_source`, `read_external_version_observations`, `destination_probe`, the validator's URL opener, `docker`, `curl` and `dotnet` are fake. Synthetic credentials, unreachable proxies and an invalid `DOCKER_HOST` keep any unfaked call offline.
- The corrective-release authority gate stays disabled (ordinary protected-environment posture), so this story does not qualify it.
- Evidence is written with CRLF so the recorded SHA-256 values survive the `text=auto eol=crlf` checkout of `.json` and `.md` files.
- Builds refuses repository-set and inventory drift between phases. A fresh verify, however, accepts any container set, and a set-preserving remap reached SDK publication. The declared profile is the control (`no-caller-set-check`), and the gap is a 30.4 finding.
- Findings handed on: `Directory.Build.targets` does not import Builds `Props/ContainerProvenance.targets` and emits only the source and version labels, so real images would fail the pinned validator (30.4). `recover-partial-release.yml` still accepts 2 or 4 images and 9 packages (30.5). `prepare-domain-release` cannot publish containers (30.3/30.4). Builds reads `id`, not `packageId` (30.3).
- The committed evidence binds source `aa097b52` with a dirty tree because this implementation is uncommitted. Regenerate it from the committed revision before requesting owner approval.
- Step 05 deviation (2026-11-01): the frozen Never forbids marking 30.2 done without owner approval, so the spec stays `in-review` and sprint status is `review`, which grants no credit. The evidence is regenerated from a clean commit before owner approval is requested.
- Evidence regenerated on 2026-11-01 from clean commit `68d0a8609b057f5aafa6975e6bac5bc69bfe3455` (dirty: no). All six results pass, and `approval.md` is pending with no not-approvable warning. Exact-revision owner approval is the remaining gate.
- Owner approval recorded 2026-11-01 in `30-2-qualification/approval.md`. The sole owner, Jérôme Piquot, holds both the Builds Maintainer and Memories Release Engineer roles. The basis is the owner's statement "i am the only owner", given in reply to the offer to record approval for both roles. Bound identities: Builds `81c88b34`, clean source `68d0a860`. The story is now `done`; approval grants no publication, predecessor or later-story credit.

## Spec Change Log

## Review Triage Log

Pass 1 (2026-11-01). Blind (B), verification-gap (V) and edge-case (E) layers. Members of a group share one row. The tree matched the pre-review snapshot byte for byte.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 package IDs checked only by count | low | rejected | True, but the profile was specified to declare the count, matching Builds `--expected-package-count`. The frozen identity already binds the ID list and manifest hash. Adding a second ID list duplicates the authoritative manifest; renames are rare. |
| B2 upstream fixtures absent from approval | medium | patch | `render_approval` never names the upstream checkpoint, which the epic AC says must receive owner approval. |
| B3/E4/E16 dirty-tree evidence approvable | low | patch | Evidence binds `aa097b52`, where the harness is untracked. The flag is shown but nothing warns approvers. Fix: a warning line, plus regeneration from a clean commit. |
| B4 two results not byte-reproducible | false | rejected | By design. The created instant comes from pinned `publish-containers.sh` `date -u`, and faking it would break the fake-only-network-seams rule. `approval.md` already says reruns need a new decision. |
| B5/V4 committed evidence never cross-checked | medium | patch | No test reads `30-2-qualification/`, so a harness edit leaves stale pass evidence and hashes. |
| B6/V5/E1 approval guard misses decisions | medium | patch | The regex matches only exact `approved`, `rejected` or `accepted`; `approve`, `declined` and `Approved with conditions` are overwritten. |
| B7 profile path label fixed | low | patch | `bindings()` always records the canonical path, whatever `--profile` points at. |
| B8/E9 `blocked` hard-coded | low | patch | `recoveryDecision.blocked` is a literal `[]` (line 2123), not derived from the absence and observe results. |
| B9 only the third member fails | false | rejected | `classify_members` does not depend on position; each member's state comes from its own directory. The frozen matrix specifies the third-image failure. |
| B10/E15 no failing package destination | medium | patch | `nugetExisting` and `probeOverrides` are never set, so no package destination is ever occupied or ambiguous. The always-green source proof is out of scope: 30.3 owns exact-source CI proof. |
| B11 30-day evidence retention | low | patch | The reference upload uses `retention-days: 30`, against recovery from immutable evidence. Add a finding for 30.4/30.5. |
| B12 refusal detail truncated | low | patch | `_bounded` keeps the usage head, so the matched `invalid choice` line is dropped from the evidence. |
| B13 qualification pin vs CI pin | low | rejected | An identical Builds SHA is 30.3's control. A gitlink bump already fails this suite; the extra cross-story assertion is not needed now. |
| B14/E17/E19 epic-context lost constraints | medium | patch | Lost the 30.4 single mapping-set identity and 30.5's owner-approved revision; the file also carries volatile story status. |
| B15 spec and sprint status disagree | false | rejected | Step 04 sets the spec `in-review`; sprint status syncs at step 05. |
| B16 offline relies on proxy variables | false | rejected | The pinned publisher runs only `date`, `docker`, `dotnet` and `python3`, and the smoke runs only allowlisted `docker`/`curl`. All are faked, and Python network goes through patched `urllib`. |
| B17 no-dry-run premise is a text search | low | rejected | The search runs over hash-pinned bytes, and any new pin is re-qualified. A behavioural proof adds complexity. |
| V1 five evidence branches untested | medium | patch | Pre-verified by a mutation run: all 48 tests passed with the five `problems.append` calls removed. |
| V2 forced self-check untested | medium | patch | Pre-verified: an unconditional `break` in `write_outputs` passed all 48 tests. |
| V3 provenance finding untested | medium | patch | Pre-verified: `_finding_provenance_labels` returning `None` passed all 48 tests. |
| V6 Builds bump turns CI red | low | patch | Intended fail-closed behaviour, but not documented at the CI step. |
| E2 decision recorded during a run | low | rejected | The run takes about 25 s, so this is unlikely, and the fix adds a second guard. |
| E3 interrupted or crashed run | low | rejected | Reference bytes are hash-verified UTF-8 at the pin, and the B5 consistency test detects mixed generations. A staging swap adds complexity. |
| E5 container discovery heuristics | low | rejected | SDK-style projects opt in per project here, so MSBuild evaluation adds complexity for an unreachable case. |
| E6 later `id` manifest rejected | low | patch | Failing closed is right, but the 30.3 handoff should name this harness as a manifest reader. Text-only fix. |
| E7 hard-coded added member | low | patch | Fix the variant to use a synthetic member that is absent from the profile. |
| E8 smoke failure not taken to recovery | medium | patch | A present, valid but unhealthy member is never shown to be blocked from recovery. |
| E10 upload-root allowlist too broad | low | patch | The prefixes `.hexalith/release/` and `src/` would hide stray files. Use exact paths. |
| E11 second upload step unchecked | low | patch | `workflow_step` returns only the first of two steps (`domain-release.yml` 552 and 1259). |
| E12 member JSON parsed leniently | low | patch | `_read_json_file` lets the last duplicate key win. |
| E13 inputs/outputs split | low | rejected | Applies only to hash-pinned bytes; a future pin is re-qualified. |
| E14 other fail-closed branches untested | medium | patch | No test triggers missing-project, missing-manifest, missing-reference, changed installed helper, early-return guards, or rejected/accepted approval rows. |
| E18 dropped pre-login revalidation | false | rejected | Not in current planning text. 30.3 says failed proofs stop before publication, and the context still says failed proofs stop the run. |
| E20 dropped secrets sentence | false | rejected | Epic 30 does not state it; the context keeps the Epic 31 OpenBao boundary. |
| E21 dropped packaged-vs-project-reference lanes | low | rejected | That is a repo-wide baseline rule, not epic-specific context. |

Pass 1 outcome: the step-03 implementer applied all 16 patch groups, and I reran verification myself:

- harness: exit 0, six results `pass`
- qualification suite: 61 passed (was 48)
- `ci_alignment`: 10 passed
- upstream `test-publish-containers.ps1`: 158 passed
- `CiTestInventoryTests`: 136 passed
- actionlint, file-scope and slice-scope gates, and the line-ending fixtures: all clean

No intent_gap, bad_spec or defer entries, so there was no loopback.

## Design Notes

Images are synthetic. Real multi-arch builds and health checks belong to 30.4. Recovery qualification proves Builds supplies enough evidence and refuses overwrite; the missing-member publisher belongs to 30.5.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p "*_test.py"` -- all pass.
- `python3 tools/qualify-builds-publication.py --output-directory _bmad-output/implementation-artifacts/30-2-qualification` -- exit 0 with six results recorded.
- `python3 -m unittest discover -s tests/tooling/ci_alignment -p "*_test.py"`; `pwsh -NoProfile -File references/Hexalith.Builds/Tools/test-publish-containers.ps1`; `actionlint .github/workflows/ci.yml` -- pass.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-10-31 | create-story | bmad-build step 02 created this spec at baseline `aa097b52`, with no test edits. Canonical ledger adopted. | Phase delta +0. Discovery was not run at create time (the planning step invokes no runner). `builds_publication_qualification` is a new scope, named here as a 0 → N transition. `ci_alignment` and `CiTestInventoryTests` baselines were deferred to dev-story discovery, with no edit to either between the phases. | Matched 1/1: the spec file only, against `git diff --name-status aa097b52a62b80af32c10267a100646b95be8ac2` plus `git ls-files --others --exclude-standard`. |
| 2026-11-01 | dev-story | Harness, profile, 48-method suite, CI step, `MANDATORY_COMMANDS` entry, evidence and planning updates. | `builds_publication_qualification`: 0 → 48 test methods, phase +48, cumulative +48, `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p "*_test.py"`. `ci_alignment`: phase +0 (one `MANDATORY_COMMANDS` entry, no test method), 10 test methods after, `python3 -m unittest discover -s tests/tooling/ci_alignment -p "*_test.py"`. `CiTestInventoryTests`: phase +0, file unmodified, 136 methods after, `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`. | Matched 16/16 against `git diff --name-status aa097b52a62b80af32c10267a100646b95be8ac2` plus `git ls-files --others --exclude-standard`. |
| 2026-11-01 | code-review | Three-layer review. 16 patch groups applied (see Review Triage Log); no defer and no loopback. | `builds_publication_qualification`: 48 → 61 test methods, phase +13, cumulative +61, `python3 -m unittest discover -s tests/tooling/builds_publication_qualification -p "*_test.py"`. `ci_alignment`: 10 → 10, phase +0, cumulative +0, `python3 -m unittest discover -s tests/tooling/ci_alignment -p "*_test.py"`. `CiTestInventoryTests`: 136 → 136, phase +0, cumulative +0, `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests`. | Matched 16/16 against `git diff --name-status aa097b52a62b80af32c10267a100646b95be8ac2` plus `git ls-files --others --exclude-standard`. |

## File List

- `.github/workflows/ci.yml`
- `_bmad-output/implementation-artifacts/30-2-pinned-builds-publication-and-recovery-qualification.md`
- `_bmad-output/implementation-artifacts/30-2-qualification/approval.md`
- `_bmad-output/implementation-artifacts/30-2-qualification/complete-set-dry-run.json`
- `_bmad-output/implementation-artifacts/30-2-qualification/evidence-upload.json`
- `_bmad-output/implementation-artifacts/30-2-qualification/partial-publication.json`
- `_bmad-output/implementation-artifacts/30-2-qualification/phase-verification.json`
- `_bmad-output/implementation-artifacts/30-2-qualification/recovery.json`
- `_bmad-output/implementation-artifacts/30-2-qualification/set-drift.json`
- `_bmad-output/implementation-artifacts/epic-30-context.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `tests/tooling/builds_publication_qualification/builds_publication_qualification_test.py`
- `tests/tooling/ci_alignment/ci_alignment_test.py`
- `tools/builds-publication-qualification.json`
- `tools/qualify-builds-publication.py`
