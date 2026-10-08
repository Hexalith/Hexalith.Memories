---
title: 'PG2 C1 authenticated GitHub bundle review consumption'
type: 'feature'
created: '2026-10-07'
status: 'done'
route: 'dispatch'
baseline_commit: 'e1c72dabc7a2a2464e34a1b46422ec2ddfbb1cda'
platform_baseline_commit: '2fe0f793a2aa3ec6a0df5e1d5c933a387ec99be4'
review_loop_iteration: 0
context:
  - /home/administrator/projects/hexalith/memories/AGENTS.md
  - /home/administrator/projects/hexalith/memories/references/Hexalith.AI.Tools/hexalith-llm-instructions.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The owner authorized preparing the concrete GitHub approval contract and implementing/testing what it supports, then said “do”. Implement read-only consumption of two separately submitted GitHub review decisions over the same immutable C1 manifest. Preserve the named-owner exception for `github:user:6775094` in Operations and Security, and producer independence. GitHub identifies the reviewer; a required, independently supplied policy determines role and scope permission.

This is a prerequisite library and contract, not an accepting qualification consumer or completion of Story 27.4. A call produces checked review observations only. Sessions, producer authentication, custody, trusted time and current role grants remain external prerequisites. No deployment, fault/purge, new consent record, staging, commit or publication is authorized here.

## Boundaries & Constraints

Reusable HTTPS retrieval belongs in Platform. C1 shape, scope, ordering, expiry and separation belong in Memories. Use standard-library Python; no new CLI, MCP server, signing issuer, dependency or key purpose. Production retrieval is authenticated GET over certificate/hostname-verified HTTPS to fixed `api.github.com`, without redirects or evidence-supplied URLs. Bound bytes and total request time, including slow responses; sanitize errors and never persist/log a token. An explicit loopback TLS fixture endpoint remains visibly different from the production issuer.

Require explicit trusted context: exact manifest reference/bytes, profile/workload/target/session, evidence source commit, reviewed PR commit, policy digest, both current role allowlists, all authenticated producer principals, trusted UTC time and numerical request/status/lifetime/review-lag limits. Supply no operational defaults or policy adoption. Two distinct review IDs on one PR bind expected principals and retained body hashes; re-fetch both each call. Unknown, missing, edited, dismissed, expired, wrong-scope or unavailable reviews refuse. No legacy label-only owner bypass, offline acceptance, execution handle or current grant inferred from GitHub author association.

## I/O & Edge-Case Matrix

| Scenario | Input / state | Behavior |
| --- | --- | --- |
| Owner in both roles | Separate approved reviews, exact bindings, current role policy, independent producers | Checked observations for both roles |
| Other repeated reviewer | Otherwise valid pair | Refuse separation |
| Changed decision | Different body hash, commit, principal or dismissed/pending state | Refuse |
| Wrong authority/context | Role missing, wrong issuer, session, scope or producer overlap | Refuse |
| Time/status failure | Untrusted caller time absent, expired, future, review before manifest, stale status | Refuse |
| Transport failure | TLS failure, redirect, HTTP denial, over-budget body, stalled request | Bounded content-free refusal |

</frozen-after-approval>

## Code Map

- `tools/access_telemetry_c1_interchange.py`: immutable bounded JSON, exact Ref matching; reuse.
- `tools/access_telemetry_c1_approval_policy.py`: owner separation predicate; reuse unchanged.
- `references/Hexalith.Platform/src/Hexalith.Platform.Custody/`: envelope authentication exists, approval-signing purpose absent; do not repurpose it.
- `references/Hexalith.Platform/eng/`: established infrastructure Python tooling owner; add importable retrieval library here, no CLI.
- `.github/workflows/ci.yml`: interchange fixtures already discovered; explicitly initialize only root-declared Platform for the new library dependency.
- `tools/verify_access_telemetry_lifecycle.py`: legacy accepting path; verify unchanged.

## Tasks & Acceptance

- [x] Add `references/Hexalith.Platform/eng/hexalith_github_reviews.py`: bounded authenticated review retrieval.
- [x] Add `tools/access_telemetry_c1_github_approvals.py`: strict C1 binding checks and immutable observations.
- [x] Add `tests/tooling/access_telemetry_c1_interchange/test_github_approvals.py`: real local TLS positive and adversarial fixtures.
- [x] Add `docs/operations/c1-github-review-contract.md`: exact wire contract, trust prerequisites, use and publication handoff.
- [x] Update `.github/workflows/ci.yml` and proposed authority/task records with accurate bounded readiness.

Given two current independently authorized role grants, when separately approved bound reviews are retrieved, then both role observations identify the stable GitHub user and exact evidence.

Given fixture or stale/edited/unavailable records, when consumption runs, then no operational authorization is created and every invalid binding refuses.

Given the unchanged legacy consumer, when this library is added, then Story 27.4, A41, Production and live prerequisites remain held.

## File Scope

Allowed to modify:

- `tools/access_telemetry_c1_github_approvals.py`
- `tests/tooling/access_telemetry_c1_interchange/test_github_approvals.py`
- `docs/operations/c1-github-review-contract.md`
- `.github/workflows/ci.yml`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/authority-and-sessions.md`
- `_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/implementation-tasks.md`
- `_bmad-output/implementation-artifacts/spec-pg2-c1-github-review-consumption.md`
- `references/Hexalith.Platform/eng/hexalith_github_reviews.py` (owned by Platform; root gitlink preserved)

## Implementation Notes

Dispatch route: no unresolved intent choice for this bounded library; no irreversible action; new cross-repository API merits the full route. User's explicit “do” supplies continuing authorization for this previously described contract, so no repeated approval request is required. Operational policy values are mandatory inputs, not invented owner decisions. Aspire startup, healthy resource inspection and stop succeeded before code changes.

Implementation design: use the fixed GET `/repos/{owner}/{repo}/pulls/{pull_number}/reviews/{review_id}`, `Accept: application/vnd.github+json`, API version `2026-03-10`. Require state `APPROVED`, numeric positive user ID with user type `User`, exact review ID, pull-request URL and reviewed commit, canonical UTC submitted time. The machine JSON body has the exact fields `schema`, `role`, `decision`, `manifest`, `profileSha256`, `workloadSha256`, `targetSha256`, `sessionId`, `sourceCommit`, `policySha256`, `expiresAtUtc`. Schema is `hexalith.access-telemetry.c1.github-bundle-decision/v1`, decision `approve`, roles `operations` and `security`; manifest uses existing Ref fields. Bind an expected SHA-256 of exact UTF-8 body bytes supplied by immutable retained references. Reject extra fields. Re-fetch observations are valid only within explicit status-age limits, counted conservatively from request start. Retain issuer, response and body digests, reviewer/role/locator/commit/expiry, but no bearer credential or grant.

GitHub forbids approving one's own PR. Actual evidence PRs therefore require an independently authorized producer/PR author different from the reviewer; this is a live prerequisite, not permission to create a bot or impersonate anyone. Primary references: https://docs.github.com/en/rest/pulls/reviews and https://docs.github.com/en/pull-requests/how-tos/review-pull-requests/approving-a-pull-request-with-required-reviews. An absolute request deadline must include DNS, TLS, headers and slow body reads. All positive/negative transport cases use real HTTPS against an explicit local fixture issuer; fixtures never report the production issuer. Missing Platform source should fail discovery, not skip tests. Keep CI dependency handoff explicit.

## Spec Change Log

- 2026-10-07: Implemented the eight permitted paths within the frozen intent.
  Trusted manifest creation time is a separate required input because the
  existing manifest shape has no timestamp. The immutable context retains a
  monotonic acquisition anchor so trusted UTC ages across repeated calls and
  dataclass replacement. No operational policy values were supplied or adopted.
  Status remains `in-progress` pending the root workflow's independent review.

## Review Triage Log

- Implementation handoff review found that reusing an old trusted UTC context
  could reset elapsed time. Fixed by retaining its acquisition anchor; the
  focused regression proves a previously valid pair expires on repeated use
  and on context replacement. Root independent review remains pending.


### Independent review, pass 1

All three configured reviewers ran with no prior conversation context. Each finding is judged separately below before shared-root-cause grouping. No finding is deferred. B2/E2 share allocation cleanup; B3/E3/E4 share finite-number validation. All retained issues are bounded corrections to existing private transport, loader, clock and fixture behavior; no public approval surface or authority policy is added.

| ID | Verdict / route | Verified evidence |
| --- | --- | --- |
| B1 worker startup backpressure | high / patch | The 65 KiB supported CA is serialized into blocking spawn startup before the deadline loop; reviewer measured 1.549 s against 0.2 s with delayed child import. Standard-library launch serializes preparation/process data synchronously. Move the existing request payload onto the already bounded nonblocking channel; retain the public API and add this actual bootstrap regression. |
| B2 allocation/constructor outside cleanup | medium / patch | Socketpair and Process construction precede try/finally. Root reproduced raw `OSError` rather than the promised closed refusal. Guard allocations and clean partial state. |
| B3 integer finite conversion | medium / patch | Root reproduced `OverflowError` for `10**400` in both request and policy constructors. Exact numeric refusal codes must cover out-of-range integers. |
| B4 transfer framing | medium / patch | Only Content-Encoding/Length are inspected. Unsupported Transfer-Encoding and chunked+Length can reach JSON admission through standard HTTP decoding. Explicitly reject unsupported/conflicting framing; retain legitimate supported chunked responses. |
| B5 source import fallback | medium / patch | Prepending a missing directory does not prevent Python from selecting another path or a cached module. The current missing-source test covers only an empty search path. Require the intended source and its module origin before using it. |
| B6 host suspension ages UTC incorrectly | high / patch | Root runtime reports `clock_gettime(CLOCK_MONOTONIC)`, which omits Linux suspend time; Python documents CLOCK_BOOTTIME as including it. Context age can undercount expiry on resume. Use a supported suspend-aware elapsed source and fail closed if unavailable; keep UTC authentication external. |
| B7 later unrelated PR review supersession | false / reject | This contract checks explicitly bound decision IDs and their current state/body, not PR merge approval or newest arbitrary review. Live authority/topology remains outside this prerequisite. A later unbound review is not a substitute for dismissal/edit of a bound decision or revocation of its external role/session grant. Document that exact distinction; do not invent a new operational supersession policy. |
| B8 exact temporal boundary cases missing | medium / patch | Current expiry test uses a time already advanced by real execution. Explicit equality-at-expiry and accepted equality at maximum lag/lifetime/status are not isolated. Add deterministic elapsed-time cases through real TLS retrieval. |
| B9 one-second reuse positive can flake | medium / patch | The initial positive requires two spawn/TLS roundtrips within one second. Busy supported runners can expire before the reuse assertion. Control decision elapsed time independently of genuine transport scheduling. |
| B10 child output not captured | medium / patch | Parent Python stream redirection cannot observe spawned OS descriptors. Capture complete child/bootstrap failure output in an outer process to exercise the credential canary, while preserving content-free failures. |
| E1 SSLKEYLOGFILE persists TLS secrets | high / patch | Local primary ssl source automatically assigns keylog_filename from SSLKEYLOGFILE; authenticated transport can therefore persist decryptable traffic secrets. Explicitly disable key logging before TLS use and verify with a real local TLS call. |
| E2 socket allocation refusal | medium / patch | Same verified allocation root cause as B2; preserve this separate finding in the log and fix once. |
| E3 deadline integer overflow | medium / patch | Same reproduced numerical-validation root cause as B3, specifically RequestLimits. |
| E4 policy/anchor integer overflow | medium / patch | Same finite conversion applies to policy limits and context anchor before range comparison; malformed construction bypasses its closed exception. |
| V1 returned scope fields unverified | medium / patch | Pre-verified in-memory target-to-workload mutation passed all 73 cases. Assert both observations' exact scope and time metadata, retaining the current correct mapping. |
| V2 production routing branch untested | medium / patch | Pre-verified wrong-production-host mutation passed all 29 new fixtures. Exercise the production constructor through isolated DNS/local TLS solely as transport; emit no production review observations. |


## Verification

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`: new TLS/decision cases plus prior wire/policy regressions.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v`: legacy behavior retained.
- File-scope, whitespace, line-ending, protected-path and staged-change checks. Record exact commands/results and the existing unrelated SQL CI inventory failure separately.

Platform publication and the matching root gitlink advance are required before a fresh pinned CI checkout can load this library. This turn prepares reviewable changes and records that handoff; it does not claim the unpublished root checkout is shippable or register an accepting provider.


### Executed implementation verification (2026-10-07)

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`:
  exit 0; 73 tests in 22.395 seconds; zero failures or skips. Includes 29 new
  real loopback TLS/decision/time/dependency fixtures and prior wire/separation
  regressions. DNS stall, TLS handshake stall, slow headers/body and trickled
  body requests terminate within their explicit fixture budgets plus bounded
  process cleanup. Fixture issuers remain distinct from production.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v`:
  exit 0; 80 tests in 32.707 seconds; zero failures or skips. Legacy behavior
  remains unchanged.
- `env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -v`:
  exit 0; 4 tests in 0.263 seconds; zero failures or skips.
- `git diff --check` and `git -C references/Hexalith.Platform diff --check`:
  exit 0. An inline Python `python3 - <<'PY'` scope/byte-policy check used
  `git diff --name-only --ignore-submodules=all`,
  `git ls-files --others --exclude-standard`, each repository's
  `git diff --cached --name-only`, and
  `git ls-files -s references/Hexalith.Platform`: exit 0; exactly the eight
  allowed paths; no staged changes; LF Python/YAML and CRLF Markdown; final
  newlines and no trailing whitespace; the root Platform gitlink remains
  `2fe0f793a2aa3ec6a0df5e1d5c933a387ec99be4`.
- `git diff --exit-code -- tools/access_telemetry_c1_interchange.py tools/access_telemetry_c1_approval_policy.py tools/verify_access_telemetry_lifecycle.py tools/verify-access-telemetry-lifecycle.py`:
  exit 0; existing wire, owner-separation policy and legacy consumers unchanged.
  Root workflow retains separate protected-path baseline receipts and final
  independent review/verification results.

Existing unrelated SQL CI inventory blocker, retained from the prior baseline
rather than rerun or repaired in this scope:

```bash
env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.CiWorkflow_RunsEveryToolingFixtureSuiteThatGuardsAShippedTool -parallelMode none -noLogo -failSkips -result-xml /tmp/pg2-c1-wire-final-qyqhy9wi/ci-inventory.xml
```

Exit 1, one failure, zero skips: `tests/tooling/access_telemetry_c1_sql_contract`
is executed by no workflow step, so reverting anything it guards ships green.
The prior receipt is `/tmp/pg2-c1-wire-final-qyqhy9wi/ci-inventory.log`; the existing
V1 deferral in `deferred-work.md` is neither changed nor duplicated.

Readiness is limited to reviewable unpublished libraries, contract and local
fixtures. No real GitHub review was consumed, accepting provider registered,
role grant/session/custody adopted, operational authorization issued, or target
contacted. No file was staged, committed or published. Platform publication and
the separately authorized root gitlink advance remain outstanding prerequisites
for a fresh pinned CI checkout; Story 27.4/A41/Production remain held.


### Root verification and matrix audit

Root retained full stdout/stderr logs and exact argv/environment/exit/time in `/tmp/pg2-c1-github-reviews-xpqfxz9u/manifest.json`, resolving the implementation handoff's transcribed-only receipts. Final current sources passed 73 interchange tests and 80 lifecycle tests with zero failures/errors/skips. No unrelated SQL lane was rerun. The exact eight-path scope guard passed.

All six frozen matrix rows are exercised in the retained interchange log: owner pair and distinct-reviewer positives; repeated non-owner refusal; body/commit/principal/state edits; role/issuer/scope/producer conflicts; missing/future/ordering/lag/expiry/context-reuse/status-age time cases; and real TLS, HTTP, redirects, bytes, DNS/headers/handshake/body deadline cases. No operational authorization result or accepting consumer exists.


### Resumed independent review, pass 2 (2026-10-08)

The three context-free layers reviewed the current eight-path artifact. Each
finding is triaged separately before grouping. The original implementation
agent is unavailable, so root applies the private corrections. There is no
intent or authority-policy change and no loopback. Prior pass-1 defects are
covered by the current sources and their 82-test interchange baseline.

| ID | Verdict / route | Verified evidence |
| --- | --- | --- |
| RB1 pinned CI lacks Platform library | medium / reject: frozen boundary | Confirmed root gitlink `2fe0f793a2aa3ec6a0df5e1d5c933a387ec99be4` lacks the file. Publication and pointer advancement are explicitly excluded from this invocation; fresh-checkout readiness remains held. An external operation advanced Platform to `7192c313edc39c6f698bc25c5475ff41d12197cd`, also returned by read-only `git ls-remote origin refs/heads/main`; the tested library bytes are unchanged. Root does not advance the pointer. |
| RB2 archived test counts differ from current sources | low / reject: spec-only | The original records describe the earlier 73/29-test pass. Current sources contain 82 interchange and 38 GitHub tests before these corrections. Preserve historical records and append exact final receipts and source hashes below. |
| RB3 historical temporary receipt absent | medium / reject: spec-only | The cited older temporary directory is unavailable in this environment. Current full logs and exact command/exit/time receipts are retained in `/tmp/pg2-c1-github-resume-f_vkqogv`; final source identities and results are also embedded below rather than treating an old receipt as current evidence. |
| RB4 invalid keylog destination processed before disabling | medium / patch | Root reproduced `IsADirectoryError` from `ssl.create_default_context()` when `SSLKEYLOGFILE` names a directory. Remove this unused environment setting in the disposable child before context construction, then assert successful real TLS for both valid and invalid destinations without file creation. |
| RB5 repository-root package import fails | medium / patch | Root reproduced `ModuleNotFoundError: No module named 'access_telemetry_c1_approval_policy'` for `import tools.access_telemetry_c1_github_approvals`. Resolve the trusted sibling tooling directory in the existing private bootstrap and document/test the actual import. |
| RB6 elapsed regression produces earlier UTC | medium / patch | The reviewer demonstrated real TLS observations with a controlled elapsed value below the retained anchor. Supported Linux BOOTTIME does not normally regress; nevertheless refuse demonstrated negative or regressing elapsed values before returning facts. No new boot identity, grant or transferable-clock policy is introduced; moving a context between clock/boot lifetimes requires a newly authenticated UTC sample. |
| RB7 incomplete-length and truncated-chunk fixtures absent | medium / patch | Current TLS fixture generates actual Content-Length and a complete terminating chunk. Add otherwise-valid envelopes with a short declared body and missing final chunk; assert closed refusals and cleanup. |
| RB8 connection-close positive/exact byte ceiling absent | low / patch | Existing no-length cases are oversized or slow failures. Add successful genuine TLS with no framing headers and exact caller-supplied byte ceiling, checking both retained response digests. |
| RB9 malformed integration envelopes already invalid for other reasons | medium / patch | Direct wire tests cover parser primitives, but malformed API fixtures often omit required review fields. Add otherwise-valid API envelopes with duplicate keys, BOM, noninteger/invalid Unicode values and string/depth violations; require the specific wire refusal. |
| RB10 independent policy digest scope negatives absent | medium / patch | The current independent-policy mismatch uses session only; decision-body digests exercise another boundary. Add all three independent policy digest mismatches and assert refusal before HTTPS. |
| RE1 blocking inherited launch metadata | high / patch | Reviewer reproduced a 1 MiB inherited process name plus delayed child bootstrap taking 1.57 seconds against a 0.2-second deadline. Bound the actual serialized bootstrap before launch so it fits the supported Linux pipe atomically; oversized inherited metadata must refuse before startup. Keep application payload transfer on the existing deadline-bound channel. |
| RE2 bounded-bootstrap claim not yet supported | high / patch | Same demonstrated launch-pipe root cause as RE1; preserve this separate finding and fix once, with the inherited-metadata/delayed-bootstrap regression. |
| RV1 incomplete response rejection unprotected | medium / patch | Pre-verified in-memory deletion of the Content-Length equality guard returned checked observations for a short, otherwise-valid response. Shares missing fixture root cause with RB7; fix once. |
| RV2 accepted zero review lag unprotected | medium / patch | Pre-verified mutation from `0 <= lag` to `0 < lag` passed all 38 GitHub fixtures. Add genuine TLS reviews submitted exactly at manifest creation and assert both observations. |

Grouping: RE1/RE2 share the blocking bootstrap; RB7/RV1 share missing incomplete
response protection. All other retained findings receive their own correction.
No finding is deferred and no protected wire, owner policy, legacy consumer,
status, deployment or gitlink bytes are changed by this build.


### Final resumed verification (2026-10-08)

Historical verification sections above describe their earlier snapshots. The
final current sources and commands are identified here. Full stdout/stderr,
argv, cwd, exit, elapsed time and before/after source hashes are retained in
`/tmp/pg2-c1-github-resume-f_vkqogv`. The final command outcomes and identities
are embedded in this permitted spec so they remain inspectable without relying
on an older environment's temporary receipts.

- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v`: exit 0; 92 tests in 27.993 seconds; zero failures/errors/skips. Receipt: `/tmp/pg2-c1-github-resume-f_vkqogv/final-interchange.json`; complete log: `/tmp/pg2-c1-github-resume-f_vkqogv/final-interchange.log`.
- `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v`: exit 0; 80 tests in 36.822 seconds; zero failures/errors/skips. Receipt: `/tmp/pg2-c1-github-resume-f_vkqogv/final-lifecycle.json`; complete log: `/tmp/pg2-c1-github-resume-f_vkqogv/final-lifecycle.log`.
- `env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -v`: exit 0; 4 tests in 0.214 seconds; zero failures/errors/skips. Receipt: `/tmp/pg2-c1-github-resume-f_vkqogv/final-line-endings.json`; complete log: `/tmp/pg2-c1-github-resume-f_vkqogv/final-line-endings.log`.

The 92-test interchange lane contains 48 GitHub TLS/decision fixtures and the
44 unchanged wire/principal regressions. The new inherited-metadata probes
cover 1 MiB parent names, import paths, argv and IPC authentication metadata
with delayed bootstrap. All refuse before launch within the explicit fixture
budget plus bounded cleanup, without orphan workers or credential output.
Otherwise-valid incomplete and corrupted API bodies refuse. Successful
connection-close bodies at the exact byte ceiling and reviews at zero lag pass.
Both observations retain the exact scope, role, body/response identity and times.
The 80-test legacy lane retains its prior denial/acceptance behavior.

Exact tested source SHA-256 identities:

| Source | SHA-256 |
| --- | --- |
| `tools/access_telemetry_c1_github_approvals.py` | `0ab431a0533a715f517ffe7bcd876b665083b0690d4515691511df735c414e90` |
| `tests/tooling/access_telemetry_c1_interchange/test_github_approvals.py` | `95233d178598346875d860126bc33f0bd9aef207f5295d60e12d3706770c6fcd` |
| `references/Hexalith.Platform/eng/hexalith_github_reviews.py` | `842af9d21c9ead55fa071b8c1bcd5da24778fb19229b5d5b3f3a8d390234c85d` |

Runtime: Linux WSL2, CPython 3.14.4. Root and Platform `git diff --check` plus
`git diff --exit-code -- tools/access_telemetry_c1_interchange.py tools/access_telemetry_c1_approval_policy.py tools/verify_access_telemetry_lifecycle.py tools/verify-access-telemetry-lifecycle.py`
returned exit 0. The exact guard command
`python3 /tmp/pg2-c1-github-resume-f_vkqogv/verify-invariants.py` returned exit 0:
eight permitted paths, unchanged tested source hashes, empty indexes, LF
Python/YAML, CRLF Markdown, final newlines and no trailing whitespace, unchanged
protected bytes and root Platform gitlink
`2fe0f793a2aa3ec6a0df5e1d5c933a387ec99be4`.

An initial stronger HEAD-invariance assertion returned exit 1 because an
external workspace operation advanced Platform from
`cf36fe7ca4ae36da9c72bb80fe9473060a4597f7` to
`7192c313edc39c6f698bc25c5475ff41d12197cd`. The review-library bytes were
unchanged across that advance. The final guard records and preserves the
external advancement and unrelated EventStore checkout change; neither was
reverted or incorporated into a staged root gitlink by this workflow.

The existing unrelated SQL CI inventory blocker remains as recorded above;
it was neither rerun nor weakened. The root-pinned Platform commit still lacks
the library, so a fresh pinned CI checkout cannot discover this new dependency.
Publish the final Platform corrections, then separately authorize and advance
the Memories gitlink before merging the root consumer/CI change.

All retained review findings are corrected and their focused regressions pass.
The final outcome is the bounded reviewable prerequisite library, contract and
fixtures. Real GitHub consumption, accepting-provider integration, live role
policy/session/custody/time prerequisites and Story 27.4/A41/Production remain
held. This build made no staging, commit, branch, dependency update or
publication action and issued no operational authorization.
