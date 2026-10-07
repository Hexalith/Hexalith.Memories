---
title: 'PG2 C1.15 runtime/control-plane producer renewal'
type: 'feature'
created: '2026-10-07'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'cc754ab1487ddcec40340f9afa370aa3114851f1'
review_loop_iteration: 1
context:
  - AGENTS.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current PG2 C1.15 is rejected; historical v1 packets lack successor provenance.

**Approach:** Extend the existing collector with a PG2-only v2 capture and define its independent-disposition contract. Complete offline producer preparation, then leave live acceptance pending.

## Boundaries & Constraints

**Always:** Exact PG2 identity/hash, Dapr 1.18.1 and authenticated index/amd64 child; all sixteen approved input hashes; immutable secret-safe neutral packets. Preserve PG1 behavior/packets, C1.16 semantics, 27.21 completion and Production/27.4/A41 states. User approved this separate scope on 2026-10-07 (“do recommended”).

**Never:** Perform live Kubernetes capture, invent reviewer authentication/approvals, build the all-gate assembler, grant C1.17/aggregate credit, register a successor, mutate deployment inputs, stage/commit/push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| :--- | :--- | :--- | :--- |
| Capture | Exact PG2 + explicit safe session, matching pods | v2 provenance; observed; no acceptance | Immutable output |
| Preflight | Invalid mode/session or configuration drift | No target call/output directory | Nonzero refusal |
| Observation | Wrong pins, malformed/secret output, missing token, timeout or identity drift | Neutral blocker; no partial observations | Bounded failure |
| Legacy | PG1 C1.15 or existing C1.16 | Existing behavior | Existing guards |

</frozen-after-approval>

## Code Map

- `tools/verify-access-telemetry-c1.ps1` — reuse inline C1.15 collection, bounded transport, strict metadata parsing, selected-Pod recheck and immutable writer. Extend ledger only for PG2 C1.15.
- `tools/access-telemetry-c1-component-backend.ps1:308` — extract sixteen-input preflight into `tools/access-telemetry-c1-profile.ps1`; reuse in both PG2 gates and record helper hashes.
- `tests/tooling/access_telemetry_c1/runtime_control_plane_identity_test.py` — existing PG1 scenarios; reuse fake kubectl in a new `pg2_runtime_control_plane_identity_test.py`.
- `tests/tooling/access_telemetry_c1/gate_c1_16_test.py:223` — replace the obsolete C1.15/PG2 denial with new required-session denial; preserve other invalid modes.
- `_bmad-output/planning-artifacts/c1-security-prerequisites-2026-10-04/predecessor-interchange-contract.md` — capture/disposition facts, no implemented authority mechanism.

## Tasks & Acceptance

**Execution:**
- [x] `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md` — define exact v2 fields/hash rules and independent disposition binding, with literal invocation and clear preparation/live-acceptance distinction.
- [x] `tools/access-telemetry-c1-profile.ps1`, `tools/access-telemetry-c1-component-backend.ps1`, `tools/verify-access-telemetry-c1.ps1` — shared preflight; exact PG2 dispatch/session; approved runtime pins; complete capture ledger and final source recheck.
- [x] `tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py`, `tests/tooling/access_telemetry_c1/gate_c1_16_test.py` — positive index/child/bare IDs, one/two pods, all preflight refusals, wrong pins/safety/drift/timeout, v2 provenance and legacy preservation.
- [x] `docs/operations/access-telemetry-adapter-production.md` — update current invocation guidance with contract link; retain dated historical claims. Append dated repository-preparation note to `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`; do not advance its matrix.

**Acceptance Criteria:**
- Given successful PG2 observation, when inspected, then v2 binds profile/workload/session/target, full source HEAD, every used source's Git-normalized and executed-byte hashes/modification state, effective invocation, actual child args/hash/start/finish/exit/safe stream hashes/result counts and overall nonzero results with zero failures/skips.
- Given source mutation during collection, when publication begins, then refuse an observed packet. A dirty development capture is labelled accurately and cannot satisfy future accepted-source checks.
- Given an independent disposition, when reviewed, then it must bind capture hash, profile/session/target, producer/reviewer independence, decision/reasons/time/expiry and retrievable authority receipt. This slice documents that contract; it never authenticates labels as authority.

## Historical Context Classification

| Source | Classification | Permitted use |
| :--- | :--- | :--- |
| 27.21/27.22 and D3 | historical-reference-only | Scope/provenance; preserve captures and decisions |
| Existing collector/fixtures | current-narrow-pattern | Reuse narrow capture behavior; rerun legacy guards |

## Slice Proof

One outcome: offline PG2 C1.15 producer preparation. Live acceptance, successor registration, other gates and bundle assembly remain independent work.

## Implementation Notes

### Review-driven implementation correction (iteration 1)

Review found loaded-source provenance and Git transport gaps. Re-derive the same v2 CLI/packet contract from the preserved prototype at `/tmp/pg2-c1-15-keep-geljnoh8`; its five mirrored source/test paths are ordinary implementation backups, not skills.

- Load helpers from one verified UTF-8 byte snapshot, keeping their expected script path semantics, and bind source hashes to that loaded snapshot. Bind the main script to its actual parsed source bytes rather than a later file read. Refuse source changes between loading and initial provenance before target calls; final recheck must still refuse later changes. Include all three sources exactly once. Do not create a new public CLI or packet surface.
- Git provenance uses at most 1 MiB per stdout/stderr and one five-second deadline covering stream reads and process exit. Refuse oversized/incomplete output, close streams and attempt owned process-tree cleanup. Preserve actual child receipts and hashes only for complete streams.
- Also fix confirmed direct defects: absolute session anchors, resolved evidence-argument safety, quoted assignments and decoded JSON string/URL safety before hashing, PG2 scalar metadata ID, deterministic Pod identity recheck, approved index/child equivalence across stable Pods, and final global dirty-state refresh. Preserve raw per-Pod image IDs; no C1.17/platform credit.
- Extend existing tests with exact workload fields and unique source-set assertions, component-helper drift, trailing newline sessions, unsafe resolved paths, quoted/escaped credentials, scalar-type refusal, benign reordered Pods, approved mixed index/child, provenance-load races and Git size/deadline refusals. New helpers remain private to this tooling.

KEEP: PG1 v1/case behavior; C1.16 neutral modes and shared preflight; all sixteen approved byte hashes; exact PG2/session/profile/workload/target bindings; successful index/child/bare and one/two-Pod captures; command/source ledgers, immutable packets, secret refusal and source drift clearing; documented independent disposition without authentication or acceptance; unchanged deployment/history/sprint/A41 state. Keep the contract, current guidance and dated preparation note, correcting only implementation facts/counts.

The previous full regression was incomplete: worker session 74466 disappeared at handoff; shared log has 15 passing methods and no final summary. It grants no full-suite credit. The parent will start and own fresh full verification after corrected focused tests return; do not background a full run and end your turn.

Aspire baseline executed with `aspire run --detach --isolated --non-interactive --apphost src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj --format Json`, source references enabled. `aspire describe` found Memories/lifecycle/clock healthy; EventStore exited 134. Stopped owned session; offline tests need no topology.

## Spec Change Log

| Date | Iteration | Trigger | Amendment / known-bad state / KEEP |
| :--- | :--- | :--- | :--- |
| 2026-10-07 | 1 | B1/E3/E6/E7 and B9/B10/E4/E5 | Clarified loaded-source snapshots and fully bounded Git transport outside frozen intent. Avoid wrong executed-source attribution and unbounded pipe drains. KEEP instructions above preserve all successful producer, neutral-state and historical behavior; direct patch findings remain required. |

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| :--- | :--- | :--- | :--- |
| B1 | high | bad_spec | The profile helper is dot-sourced before initial Git queries/hash reads. The reproduced first-query mutation is adopted as baseline, so final unchanged does not establish the executed source bytes. |
| B2 | high | patch | The raw evidence argument is scanned before absolute resolution. A safe relative argument under a canary-bearing working directory copies unsafe resolved text into producer.arguments; resolve and scan before provenance/target calls. |
| B3 | high | patch | The new assignment regex excludes quotes immediately after =. The reproduced quoted password diagnostic reaches validated hashes/observed status; accept optional quoting in the existing secret rejection. |
| B4 | medium | patch | The metadata id is cast before equality. PowerShell casts its one-element array to the expected string; validate the scalar type in PG2 before casting. |
| B5 | medium | patch | The entire selected-Pod array is compared in returned order. Reversing unchanged Pods produces a false drift blocker; compare the existing selected identity facts deterministically by Pod name. |
| B6 | medium | patch | Each approved index/child digest passes per-Pod pins, but the legacy unique-digest aggregate rejects their authenticated equivalent relationship. PG2 should preserve both raw IDs while admitting this one approved image family; raw per-Pod recheck stays exact. |
| B7 | medium | patch | The global dirty flag is sampled only initially. A later harmless .gitattributes change leaves a clean label. Refresh Git dirty state at final recheck; normalization changes already fail actual Git blob comparison, so adding a new configuration registry is unnecessary. |
| B8 | low | reject | Ancestor directory symlinks can preserve exact approved bytes and produce a neutral dirty-development capture. Every input hash and final source check still apply, and such captures cannot meet accepted-source checks. Additional ancestor-link traversal is disproportionate to this uncommon developer-only case; leaf symlink refusal remains. |
| B9 | medium | bad_spec | New Git CopyToAsync streams buffer without a cap. The reproduced multi-MiB output is accepted, so status/filter output can exhaust memory; bound both streams in the existing child collection design. |
| B10 | medium | bad_spec | WaitForExit only bounds the Git parent. GetResult then drains inherited pipes without a deadline; the reproduced exited parent/lingering child exceeds five seconds. Use one deadline for both streams and process completion, with best-effort owned-process cleanup. |
| E1 | medium | patch | The .NET $ anchor accepts a final LF. The reproduced session reaches target calls and observed output; use absolute string anchors and cover LF/CRLF. |
| E2 | high | patch | The decoded guard decodes Unicode but not escaped slashes or arbitrary JSON string values. Reproduced credential URLs pass before hashes are validated; scan decoded string content on both streams before hashing. |
| E3 | high | bad_spec | Same demonstrated loaded-source/baseline race as B1; hashing the current file after dot-sourcing does not identify the loaded helper. |
| E4 | medium | bad_spec | Same unbounded Git stdout/stderr buffer as B9; both streams require a fixed cap before any full-stream hash. |
| E5 | medium | bad_spec | Same inherited-pipe drain gap as B10; process exit alone does not bound completion. |
| E6 | high | bad_spec | The executedBytesSha256 claim is contradicted by B1/E3 reproduction: replacement bytes are recorded while earlier helper code runs. |
| E7 | high | bad_spec | The initial provenance phase is inside collection. Mutation there is accepted as baseline, contradicting source-mutation refusal; resolve through verified loaded snapshots. |
| E8 | high | patch | The safe-stream claim is contradicted by E2: metadata blocks only after unsafe hashes were marked validated, and stderr can stay observed. Move complete decoded scanning before hashes. |
| V1 | medium | patch | Pre-verified regression gap: positive/provenance tests never assert workloadId/workloadSha256. Add exact approved binding assertions to the existing positive cases. |
| V2 | medium | patch | Pre-verified verification gap: count 19 allows duplicate helper paths and omits component-helper mutation coverage. Assert the exact unique path set and cover all three loaded producer/helper mutations. |
| B2.1 | high | patch | Reproduced repository-root canary enters sourceCommands.arguments via hash-object absolute path; validate repositoryRoot before any retained source arguments/Git calls. Same root cause as E2.2. |
| B2.2 | high | patch | Reproduced embedded JSON name=DAPR_API_TOKEN/value pair receives validated stream hashes. Existing recursive credential screening must cover embedded objects and credential name/value pairs; group with E2.3/E2.6. |
| B2.3 | medium | patch | Verified final global status is sampled before nineteen Get-C1SourceState calls. /tmp/pg2-review2-final-dirty-repro.txt demonstrates a late .gitattributes change remains clean; refresh after source queries. |
| B2.4 | medium | patch | /tmp/pg2-review2-repros.txt reproduces resourceVersion-only running-pod-changed. Whole metadata/status equality includes incidental facts; compare a sorted qualification identity/stability projection, retaining lifecycle and daprd image/ready, phase, UID and deletion checks. |
| B2.5 | medium | patch | Reproduced array label and Ready.status coerce to expected strings and pass. Require scalar strings for selected label, condition type/status and container names in PG2 initial/recheck projections. |
| B2.6 | medium | patch | Reproduced UID/image/scheduler trailing LF passes .NET $ anchors. Use absolute anchors in PG2 identity/pin/metadata array patterns; retain PG1 behavior. Same image claim as E2.4. |
| B2.7 | low | reject | Invoke-C1SourceGit already bounds both streams/exit, closes inherited streams and attempts Kill(true). Owned descendant cleanup after a parent has exited is explicitly best effort. Reliable cross-platform process-group supervision adds state/platform behavior for an uncommon failing local Git wrapper; captures already refuse and no authority credit is granted. |
| B2.8 | low | reject | UTC deadline arithmetic can extend elapsed duration after a backward clock step. That system-clock adjustment is uncommon in this offline preparation slice. Adding separate elapsed-time state to both transports is disproportionate here; ordinary hung-parent/inherited-pipe deadlines have direct passing tests. |
| B2.9 | low | reject | Standard SHA256.HashData consumes exact captured byte arrays directly; current tests independently recompute context/version/empty stderr/Git show, arguments and source hashes. Exhaustive fixture duplication for every standard-library hash mirrors implementation without demonstrating a distinct transform defect. |
| B2.10 | low | reject | Write-ImmutablePacket is unchanged baseline code; a write/flush failure can leave an incomplete file but propagates nonzero and never returns a published path. Partial-write recovery requires new cleanup branches/fault injection for an uncommon filesystem-failure case, with no acceptance authority in this neutral producer. Reject as disproportionate; independently authenticated packet approval remains pending. |
| E2.1 | high | patch | Verified final source loop checks each path once, allowing a later Git query to mutate an earlier checked path. Add a final cheap byte-snapshot sweep after all Git queries and final HEAD check before publication; group with E2.5 and B2.3. |
| E2.2 | high | patch | Same retained repository-root credential-shaped argument as B2.1; currently repositoryRoot is never screened. |
| E2.3 | high | patch | A decoded string containing JSON password fields receives only assignment/URL screening, while raw-property regex runs on the original text. Apply existing decoded credential-property screening to embedded string content before stream hashes; group with B2.2. |
| E2.4 | medium | patch | Same reproduced approved-image trailing LF accepted by $ anchors as B2.6; absolute PG2 pin/identity anchors. |
| E2.5 | high | patch | Same final-query source-mutation/unchanged claim as E2.1, verified from final source loop; closing loaded snapshot sweep required. |
| E2.6 | high | patch | Same validated-unsafe-stream claim as E2.3; credential screening must run before retained stream hashes. |
| V2.1 | medium | patch | Pre-verified gap: disposable mutation of profileIdentity and target.appId still passes provenance test because targetSha256 is only recomputed from emitted data. Assert approved profile identity and complete target literal before their hashes. |

## Verification

Run `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p '*_test.py' -v`; require nonzero cases, zero failures/errors/skips. Reuse canonical lifecycle/17-architecture verification for integration boundaries. Check both whitespace and declared diff. No live capture.

### Initial implementation verification (superseded)

- Corrected PG2 lane: 13 passed, zero failures/errors/skips; `/tmp/pg2-c1-15-corrected-focused.log`, SHA-256 `bd2089edb10444576e53ff5cc6c8da3e87e111bf4cac7912ed0e0ff7d72208ac`.
- Required-session mode guard: 1 passed; `/tmp/pg2-c1-15-session-denial.log`.
- Canonical boundary receipt `/tmp/story-27-4-offline.7ETQL5cv`: 79 lifecycle tests, exactly 12 + 5 architecture guards, all successful; Debug/source-reference build had zero warnings/errors; all ten commands exited 0.
- All three PowerShell sources parse cleanly. Tracked and newly added file whitespace checks passed.
- Frozen matrix coverage: capture → index/child/bare one/two-Pod test; preflight → invalid-mode/session and sixteen-input refusal tests; observation → pins/token/secret/UTF-8/identity/source-drift and bounded-transport tests; legacy → PG1 v1 and existing C1.16 mode guards. Each covering PG2 test ran in the corrected 13-case lane.
- Earlier 99-case verification was incomplete: worker session `74466` disappeared, leaving 15 passing methods and no final summary in `/tmp/pg2-c1-15-corrected-all-tests.log`. That run grants no full-suite credit. Fresh parent-owned verification uses 104 discovered cases after re-derivation.
- No live target capture, authority/approval implementation, successor registration, deployment mutation, staging, workspace commit or push occurred.

### Re-derived implementation verification (2026-10-07)

Corrected focused command `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p 'pg2_runtime_control_plane_identity_test.py' -v`: 18 cases passed in 303.412s, zero failures/errors/skips. Receipt `/tmp/pg2-c1-15-rederived-corrected-focused.log`, SHA-256 `da80b356b1f1e2a2dcfea4e8f0d59a455a47925a5774e52d792dd97a5fb92996`. Required-session legacy guard: 1 passed, `/tmp/pg2-c1-15-rederived-session-denial.log`.

Parent read the baseline diff including untracked files and the full iteration update. All four execution tasks and three acceptance criteria are implemented; frozen matrix rows are covered by the corrected positive, preflight-refusal, observation/safety/drift/transport and legacy tests, each run and passed. Exact workload fields, nineteen unique source paths, all three initial/load/final source races, dirty refresh, reordered/mixed authenticated digests and both Git stream/deadline cases now have direct assertions. Fresh parent-owned canonical boundary receipt `/tmp/story-27-4-offline.ooBUC5cF` passed: 79 lifecycle cases and exactly 12 + 5 architecture guards, all ten commands exited 0, Debug/source-reference build had zero warnings/errors. Architecture XML SHA-256 `f5b06f8486885cb6338c67bf739580c22f54e7f125c38716dc2956b15cb6a170`. Parent-owned full C1 session 36481 was stopped with exit 130 before completion when second review required source fixes; it grants no full-suite credit. Full C1 verification will run after the narrow fixes; no live acceptance is claimed.


### Review patch verification (2026-10-07)

All three second-review layers reported before triage. Seventeen findings were individually classified, then grouped; confirmed direct defects were patched through the same implementation worker. Four uncommon low-impact findings were rejected as disproportionate, with evidence recorded above. No findings were deferred. Parent independently read the patched sources and new regression methods.

Focused review-patched verification: 22 PG2 unittest cases passed in 547.824s, zero failures/errors/skips; `/tmp/pg2-c1-15-review-patch-focused.log`, SHA-256 `3edf582c882aaf8eed3dbe64532d8709b5759ab4f33a82b8bde6ecee4f82a9ac`. Narrow C1.16 valid-capture/secret-reference checks: 2 passed in 35.459s, zero failures/errors/skips; `/tmp/pg2-c1-15-review-patch-c1-16-targeted.log`, SHA-256 `478fac17978cd3d8d21625cc019e6fc51ac1bb5ceb83abb2e3604833d3dcd4b9`. All three sources parse, whitespace and CRLF PowerShell/LF Python checks passed.

Second-review patches add final no-child byte-snapshot checks after source queries and dirty refresh, repository-root screening, embedded/name-value credential screening, deterministic typed qualification Pod projection, absolute PG2 identity anchors and exact profile/target assertions. Discovery is 108 C1 unittest cases = baseline 86 + 22 PG2 cases (review phase +4 from 104), using the unchanged discovery command above. Full parent-owned verification is running with receipt root `/tmp/pg2-c1-15-reviewed-verification-lo9pmav6`; no completion credit until its exit and terminal summary are verified.

Fresh final-source canonical receipt `/tmp/story-27-4-offline.jZZMVwe1`: all ten logged commands and block exited 0; 79 lifecycle tests passed, exactly 12 retention-decision + 5 A41 guards all Pass, zero failures/errors/skips/not-run. Debug/source-reference build: zero warnings/errors. XML SHA-256 `945fab28cbdd337e292f17bc57c5a3305997b54d968057d01624634a3f2d7fa9`; assembly SHA-256 `8c8bda61518c0034678e066d2dca26d5a067a770c33640ac1f1b6625af5ab7d9` is build identity only. Protected-file reconciliation: 72/72 unchanged.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :--- | :--- | :--- | :--- |
| 2026-10-07 | create-story | User-approved separate preparation scope; no sprint registration. | Delta +0; baseline 86 unittest cases, loader.discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases(); future added lane recorded separately. | matched 2/2 against baseline, including untracked files: this spec and the 27.4 decision handoff. Reconcile implementation paths after development. |
| 2026-10-07 | dev-story | Prepared the PG2-only v2 producer, shared exact input preflight, disposition binding contract, current guidance and dated preparation note. Corrected the fake UTF-8 hook and made dirty-source expectations valid in clean checkouts. Full regression/review remain pending at handoff. | Delta +13; 99 discovered = 86 existing + 13 new. Corrected PG2 lane: 13 passed; mode guard: 1 passed; canonical boundaries: 79 lifecycle + 17 architecture passed. | matched 10/10 against current changed/untracked paths: nine owned preparation files plus the related 27.4 decision handoff. |
| 2026-10-07 | dev-story | Re-derived loaded snapshots/bounded Git transport and confirmed direct fixes; expanded refusal/binding assertions. | Phase +5; cumulative +18; complete C1 discovery 99 -> 104 unittest cases, PG2 scope 13 -> 18. Count: unittest.TestLoader().discover('tests/tooling/access_telemetry_c1', pattern='*_test.py').countTestCases(); corrected focused command above ran all 18 PG2 cases, zero failures/errors/skips. | matched 10/10; baseline cc754ab1487ddcec40340f9afa370aa3114851f1; git diff --name-only plus git ls-files --others --exclude-standard, including related 27.4 decision handoff. |

## File List

- `_bmad-output/implementation-artifacts/spec-pg2-c1-15-runtime-control-plane-renewal.md`
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-5.md`
- `docs/operations/access-telemetry-c1-pg2-c1-15-contract.md`
- `tools/access-telemetry-c1-profile.ps1`
- `tools/access-telemetry-c1-component-backend.ps1`
- `tools/verify-access-telemetry-c1.ps1`
- `tests/tooling/access_telemetry_c1/pg2_runtime_control_plane_identity_test.py`
- `tests/tooling/access_telemetry_c1/gate_c1_16_test.py`
- `docs/operations/access-telemetry-adapter-production.md`
- `_bmad-output/implementation-artifacts/tests/27-4-retention-verification-evidence.md`

Remediation runtime checklist: not applicable — read-only operator capture; no domain workflow, shared-state cleanup, migration or rollback behavior.
