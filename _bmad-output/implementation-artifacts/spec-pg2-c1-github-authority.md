---
title: 'PG2 C1 GitHub-backed policy and session authority'
type: 'feature'
created: '2026-10-08'
status: 'done'
route: 'dispatch'
baseline_commit: 'aac6d9054cb138881e6e49c8e48233553123ffce'
platform_baseline_commit: '794e8c63fe945bf689064edb8a406801810cb4cf'
review_loop_iteration: 0
context:
  - /home/administrator/projects/hexalith/memories/AGENTS.md
  - /home/administrator/projects/hexalith/memories/references/Hexalith.AI.Tools/hexalith-llm-instructions.md
  - /home/administrator/projects/hexalith/memories/_bmad-output/project-context.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The owner chose “do recommended”: implement a narrow GitHub-backed policy/session adapter, integrate role grants with existing bundle consumption, and prove one gate-review chain through real TLS fixtures. Capture execution provenance remains independently required for accepted evidence.

## Boundaries & Constraints

Platform owns generic review authentication; Memories owns C1 rules. Use existing transport/standard-library Python, without new service/CLI/issuer/keys/dependencies/workflow. Work in owning repositories. Preserve legacy consumers, gate/status/history/A41/runtime, prior dirty draft and concurrent untracked planning file. No Git mutation, target operation or actual grant.

Require an independent bootstrap root (issuer/repository/policy reviewers, trusted UTC/acquisition anchor, request/status/expiry bounds); policy cannot appoint its own root. Authenticate policy, then scoped session/reviewer grants. All limits are explicit; fixtures adopt no operational policy. Require exact snapshot refs, review IDs/PR commits, PG2 profile/workload/target/source/session bindings, permitted gates/actions/principals, canonical UTC ordering/expiry and parent dependencies. Refuse cycles, mixed scope, escalation and reviewer/producer overlap; preserve the named-owner two-bundle-role exception and separate decisions. Re-fetch all dependencies each call and enforce earliest expiry/status freshness across the entire chain, including bundle retrieval, context reuse and suspend-aware elapsed time. Produce immutable checked observations only; no accepted-gate, predecessor or execution handle.

## I/O & Edge-Case Matrix

| Input/state | Result |
| --- | --- |
| Root-approved policy, permitted session/gate review and exact refs | Authenticated immutable observations; genuine TLS fixture chain passes |
| Missing root/role/session, self-appointed root, wrong target/tenant/action or producer overlap | Refusal before later dependent review calls |
| Edited/dismissed/unavailable review, changed bytes/commit/principal, unknown schema/fields or cyclic refs | Bounded content-free refusal; no partial result |
| Expired/future/ill-ordered decision, stale first dependency during later calls or elapsed regression | Refuse; context reuse never renews authority |
| Actual capture provenance/custody or live grant missing | No gate pass/execution permission inferred from review observations |

</frozen-after-approval>

## Code Map

- Platform `eng/hexalith_github_reviews.py`: pinned bounded HTTPS retrieval, `GitHubReviewClient`, `ReviewLocator`, `RequestLimits`, suspend-aware `_elapsed`; reuse unchanged.
- `tools/access_telemetry_c1_interchange.py`: reuse snapshots/Refs.
- `tools/access_telemetry_c1_github_approvals.py`: existing `CurrentRolePolicy`, `ApprovalContext`, `ReviewBinding`, `consume_bundle_reviews`; preserve compatibility and regression evidence.
- `tests/tooling/access_telemetry_c1_interchange/test_github_approvals.py`: real loopback TLS fixture/certificates and existing negative cases; reuse fixture infrastructure.

## Tasks & Acceptance

**Execution:**
- [x] Platform `eng/hexalith_github_decisions.py` — immutable generic exact-review/body authentication over existing transport; bounded strict parsing/sanitized errors, no C1 schemas.
- [x] `tools/access_telemetry_c1_github_authority.py` — closed policy/session/gate-review schemas and root/parent-chain checks; derive bundle role context from verified grants and revalidate whole-chain expiry/status afterward. Manifest creation/producer/custody/target facts stay independently supplied.
- [x] `tests/tooling/access_telemetry_c1_interchange/test_github_authority.py` — real TLS positive grant/gate/bundle chains and matrix negatives, early dependency/cross-target/tenant/session denials, clock reuse/latency/suspension, redaction, malformed envelopes and isolated pinned imports; no mocked success verdict.
- [x] `docs/operations/c1-github-authority-contract.md` — exact schemas/bootstrap/limits/withdrawal/revalidation and collector handoff; runnable fixture/API examples, no operational defaults/fabricated receipts.
- [x] `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/authority-and-sessions.md`, `implementation-tasks.md` — append scope/evidence/owner direction; preserve unresolved prerequisites/history.

**Acceptance Criteria:**
- Given authentic root-approved policy/session decisions, when the adapter consumes fresh bound GitHub responses, then their permitted roles and scope drive existing bundle checks without caller-invented role grants.
- Given altered, unauthorized, expired or unavailable dependencies, when any chain is consumed, then it yields no partial observation/result and makes no target/deployment call.
- Given completed code, when inspected, then grants and executor provenance remain distinct; protected bytes/27.4/A41/Production remain unchanged.

## Implementation Notes

Platform owns generic exact review/PR commit/principal/body authentication and
immutable bounded JSON, with no C1 schema dependencies. Its existing transport
is unchanged. Memories owns explicit independent bootstrap issuer/repository/
policy reviewers/time acquisition/limits, closed scoped policy/session/gate
schemas, subset review grants and policy-defined acyclic gate parents. Current
published PG2 profile/workload hashes are required at scope construction;
historical PG1 and inexact identities cannot acquire a PG2 label.

Authenticated session grants derive the existing bundle role context. All
ancestors are re-fetched every invocation and rechecked after each dependency
and after bundle retrieval for earliest expiry, review window and status age.
Context reuse preserves the original suspend-aware clock anchor; regressions
across generic/legacy bundle consumption boundaries refuse. Named-owner dual
bundle roles retain distinct decisions and producer exclusion.

Actual capture provenance/custody/target identity and manifest creation/producers
remain independent trusted inputs. Returned facts grant no execution, admit no
gate/predecessor and resolve no operational C1.23–25 dependency topology.
Publication of the new Platform file and a separately authorized matching root
gitlink advance remain outstanding. No Git mutation or target operation occurred.

## Spec Change Log

## Review Triage Log

| ID | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| B1 | medium | Valid manifests newer than the root UTC sample fail after four authentic ancestor calls because ApprovalContext receives the original UTC; root reproduced the TLS refusal. | patch P1 |
| B2 | false | The contract explicitly bounds the complete decoded API review body to 4096 characters; cardinality maxima are additional ceilings, not a promise that every combination fits. A 6012-byte envelope body correctly refuses the supported string budget. | reject |
| B3 | low | Bundle bodies bind session ID and policy digest, while the returned ancestor observation separately binds the exact session Ref. The contract should explicitly state that bundle reviewers have not endorsed an arbitrary replacement grant and collector adoption must verify one immutable grant Ref per session ID. | patch P2 documentation |
| B4 | medium | Gate capture finish equals collection end and passes <=, although the contract describes end boundaries as exclusive. A direct comparison correction and equality refusal test align the existing supported window rule. | patch P3 |
| B5 | medium | A 26-request tuple with a sufficiently large independent dependency budget retrieves policy/session before the existing _gates limit refuses. Entry validation can reject this already-invalid shape before producer aggregation or requests. | patch P4 |
| B6 | medium | Root isolated reproduction loaded JsonSnapshot from a cached unrelated checkout successfully. That can make this adapter use different wire or separation rules; the new import boundary needs the same exact-origin checks as Platform. | patch P5 |
| B7 | medium | Only policy expiry is advanced during bundle retrieval. Gate expiry and session review-end checks lack independent seam regressions; session expiry can equal, but never validly precede, review-end because bootstrap enforces reviewEnd <= expiry. Add valid distinct boundary cases. | patch P6 tests |
| B8 | medium | The supported parent traversal is tested only linearly. Shared-parent retrieval, reordered branching, maximum request count and dependency-budget equality need positive TLS coverage for the advertised traversal/bounds; no additional production mechanism is needed. | patch P7 tests |
| B9 | low | Standalone generic authentication checks an invalid started sample only after retrieval. Move that existing validity check before the request and prove no request for NaN/negative acquisition; retain the final regression check. | patch P8 |
| B10 | low | Source hashes are retained in the explicitly chosen temporary receipt directory; the proposed fix edits this build spec. The review workflow explicitly rejects findings whose fix edits the build spec. Publication and permanent operational receipts remain outside this prerequisite. | reject spec-edit finding |
| E1 | medium | The edge review independently reproduced the same original-UTC ApprovalContext rejection described in B1; the advanced chain timestamp already demonstrates a valid manifest. | patch P1 shared cause |
| V1 | medium | Preverified mutation removed per-principal subset enforcement while all 127 tests still passed. Globally supported permissions withheld from a known principal are absent from current tests; a real TLS Security-role escalation demonstrates the protected harm. | patch P9 tests |
| V2 | medium | The verification review independently reproduced B1 with manifest +30 s, current +60 s and valid approvals +40/+45 s; constructor rejects before either bundle review. | patch P1 shared cause |

All findings were classified individually before grouping. B1/E1/V2 share the
original-UTC handoff defect. The other retained entries are direct corrections
or verification/documentation additions with no new public surface. No intent
gap, bad-spec loopback or deferred-work mutation is required.


Patch resolution: P1 advances the bundle UTC/acquisition pair equivalently while
preserving the root anchor; P3 makes collection end exclusive; P4 rejects invalid
counts before processing; P5 pins all local dependency origins; P8 checks the
initial generic clock before HTTPS. P2 documents exact-session-Ref limitations
and collector registration requirements. P6/P7/P9 add the corresponding genuine
TLS expiry/window, shared-parent/max-count/budget and per-principal grant tests.
All nine retained entries are fixed and independently inspected in
`root-review-fix-delta.diff`. Nothing was deferred; no review loopback was needed.

## Verification

Run `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`; baseline 92 cases, all pass. Run lifecycle lane (baseline 80), line-ending checks and `git diff --check`; retain commands/logs/source/counts in `/tmp/pg2-c1-authority-lbzyqupl`. Attribute only new authority cases; inspect Platform diff separately.

Before code, `aspire run --apphost references/Hexalith.Platform --detach --non-interactive --format Json` exited 2 because its nested Builds import is missing; no nested initialization. `aspire describe` confirmed no running host. Verify the library independently.


Implemented verification (2026-10-08), retained in
`/tmp/pg2-c1-authority-lbzyqupl`:

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — 127 passed, zero failures/skips, 42.890 s (`interchange-full.log`): 92 unchanged baseline plus 35 new authority tests.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — 80 passed, zero failures/skips, 39.036 s (`lifecycle.log`).
- The authority-only lane passed 35 cases (`authority-focused.log`). A later added assertion to the existing elapsed-regression test passed its focused `-k test_elapsed_regression_unsupported_clock_and_unavailable_time_refuse` run (`elapsed-seam-regression.log`). No implementation source changed after the full lane passed.
- Both documented targeted fixture commands passed one case each (`contract-bundle-example.log`, `contract-gate-example.log`). Positive verdicts used genuine certificate-verified HTTPS, with no mocked provider success.
- Cross-target/tenant/session/profile/workload/source tests assert denial before dependent review calls; grant escalation, role/action absence, producer overlap, unknown schemas/fields, edited/dismissed/unavailable reviews, PR commit/principal changes, parent Ref/dependency failures, future/order/lifetime/expiry, old-context reuse and whole-chain status ageing all have focused assertions.
- Exact source SHA-256 values, commands/results, line-ending checks, protected-file hash comparison and separate Platform diff are retained in the receipt directory. Existing transport/bundle/policy/wire modules, protected status/history/27.4/A41/runtime/Production files, prior dirty draft and concurrent untracked planning bytes are preserved.

Protected-file verification: 9/9 unchanged starting hashes, including the
concurrent readiness file. The root agent intentionally appended the approved
provider decision and authority-spec link to the prior dirty 27.4 draft before
dispatch; that attributed metadata write retains draft status, and the
implementation agent never wrote that file. Both repositories and all new files
pass whitespace checks, Python syntax checks and declared line-ending policy.
The two prerequisite handoff files retain the entire original content as a prefix.

Root verification: latest full interchange run passed all 127 cases in 46.227 s,
including every one of the 35 new cases with zero skips (`root-interchange-full.log`).
All five matrix rows map to executed passing tests in `matrix-audit.json`; all
five execution tasks and three acceptance criteria were checked against the
complete root/Platform unified diff, including untracked files. The repository
line-ending lane passed (`root-line-endings.log`), and the nine protected-file
hashes were independently rechecked unchanged (`root-protected-check.json`).
The root also corrected the draft's historical-only preservation note after
recording the authorized provider decision; Story 27.4 stays draft.

Final post-review verification, 2026-10-08:

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v` — exit 0; 135 passed, zero failures/skips, 39.616 s (`final-interchange.log`): 92 existing cases plus 43 authority cases.
- `env PYTHONDONTWRITECODE=1 PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v` — exit 0; 80 passed, zero failures/skips, 31.424 s (`final-lifecycle.log`).
- `env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -v` — exit 0; 4 passed, 0.288 s (`final-line-endings.log`).
- Root and Platform `git diff --check`, plus untracked-file whitespace, Python syntax and declared byte/line-ending checks — all pass. Both prerequisite handoffs retain their complete original prefixes.
- Every one of the 43 new tests ran and passed; the five matrix rows and all review fixes were audited against the final logs and source (`final-matrix-audit.json`). Nine protected hashes still match the starting receipt (`final-protected-check.json`).
- Original Aspire baseline command/result is retained in `aspire-baseline-result.txt`: startup exited 2 on missing nested Builds `Directory.Packages.props`; no host ran or nested dependency was initialized.

The prerequisite is complete as local reviewed code/documentation. No commit,
staging, push or gitlink update was made: the approved prerequisite boundary
expressly excludes Git mutation. Publication remains a separate handoff.
Story 27.4 stays draft; A41/Production and actual grant/capture/custody/consumer
prerequisites remain held.
