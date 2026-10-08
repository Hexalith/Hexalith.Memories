# C1 GitHub bundle review consumption contract

This prerequisite library checks two separately submitted GitHub reviews over
one frozen C1 manifest. It returns immutable observations for Operations and
Security. It supplies no qualification authorization, session, role grant,
capture authentication, custody proof, execution handle or deployment consent.
Story 27.4, A41, Production and the remaining live prerequisites stay held.

## Ownership and supported retrieval

Platform owns the reusable importable transport in
`references/Hexalith.Platform/eng/hexalith_github_reviews.py`. Memories owns the
C1 checks in `tools/access_telemetry_c1_github_approvals.py`. The existing wire
and named-owner principal policy modules are reused unchanged. No new CLI,
MCP server, signing issuer, key purpose or external Python dependency is added.

Production constructs `GitHubReviewClient()` and always sends an authenticated
GET to certificate- and hostname-verified `api.github.com:443`, using the system
CA store and this fixed path:

```text
/repos/{owner}/{repository}/pulls/{pull_number}/reviews/{review_id}
```

Headers are `Accept: application/vnd.github+json`,
`X-GitHub-Api-Version: 2026-03-10`, `Accept-Encoding: identity`,
`Cache-Control: no-cache` and `Authorization: Bearer <ephemeral credential>`.
Owner/repository and positive integer resource IDs are validated before use.
No evidence URL, redirect, environment proxy, anonymous retry or cached/offline
response selects or supplies a resource. Only HTTP 200 with an unencoded body
is supported. Ordinary Content-Length, connection-close and chunked framing
are supported; unknown Transfer-Encoding and chunked plus Content-Length
refuse. The disposable worker removes `SSLKEYLOGFILE` from its own environment before
creating the TLS context and explicitly disables key logging before the handshake.
No keylog destination is opened; an invalid destination cannot disable retrieval.
The parent environment remains unchanged. Denials, malformed HTTP, excess body bytes and unavailable TLS
refuse with closed diagnostic codes that contain no remote text or credential.

`RequestLimits(max_response_bytes, deadline_seconds)` requires both values.
The body ceiling is at most 1,048,576 bytes; standard-library HTTP header count
and line bounds also apply. A short-lived spawned process performs each request.
A suspend-aware absolute deadline starts before worker launch and includes
bootstrap, request-parameter transfer, DNS, connect, certificate verification,
TLS, headers and slow body reads. The socket handle and inherited framework metadata enter the minimal
spawn bootstrap. Before launch, inherited names, paths, arguments and IPC
authentication metadata receive bounded structure checks and the complete
bootstrap is measured using the same standard-library serializers. More than
4096 serialized bytes refuses before startup; the accepted bootstrap fits the
Linux pipe without waiting for child import. The caller must keep its process
and import metadata stable during launch; concurrent changes to that state
are unsupported. This private preflight uses CPython multiprocessing internals
and the supported runtime is Linux CPython. Request parameters, including the credential and at most
65,536 ASCII CA characters, travel in a bounded 400 KiB control frame over the
nonblocking channel after startup. The parent can interrupt every application
request transfer at its deadline; response bytes and closed errors use the same
channel. Socket/process allocations and partial setup are guarded and cleaned
up on refusal. At timeout the worker is killed; cleanup joins for at most 0.1 seconds. OS scheduling/cleanup can add
small overhead to the requested deadline; it is not a hard real-time guarantee.
The credential stays in transient process memory/IPC and never enters process
arguments, client state, observations, logs or persisted files. The library does
not fetch, issue or renew credentials.

The REST endpoint supplies reviewer identity and review state; GitHub metadata
such as `author_association`, display name and login supplies no scope or role
grant. See [GitHub's review API](https://docs.github.com/en/rest/pulls/reviews).

## Independently authenticated caller inputs

All inputs are mandatory trusted facts. Creating one of these Python objects
checks structure; it does not authenticate the caller's assertions or adopt a
policy. An external authority adapter must supply and authenticate the facts.

| Object | Required facts |
| --- | --- |
| `ApprovalContext` | Exact `EvidenceReference` and immutable manifest bytes; separately authenticated `manifest_created_at_utc`; exact profile, workload and target SHA-256 values and session ID; evidence `source_commit`; PR `reviewed_commit`; `CurrentRolePolicy`; all authenticated capture producer principals; trusted aware UTC `trusted_now_utc`; explicit `expected_issuer`; `RequestLimits`. |
| `CurrentRolePolicy` | Independent current policy digest and exact profile/workload/target/session scope; current nonempty immutable `operations_principals` and `security_principals` allowlists; positive finite `max_status_age_seconds`, `max_approval_lifetime_seconds`, `max_review_lag_seconds`. |
| Each `ReviewBinding` | Exact `ReviewLocator`, expected canonical GitHub principal, retained body `EvidenceReference` and its exact immutable UTF-8 JSON bytes. |
| Credential | Authenticated read access to the expected evidence repository through the external approved secret boundary. |

`source_commit` and `reviewed_commit` are distinct bindings even if a particular
evidence workflow uses the same value for both. They require full lowercase
40-digit commit hashes; this library does not prove their source provenance or
query PR head status. The authority adapter must select the exact expected PR
commit. Scope digests require 64 lowercase hexadecimal digits. Session IDs
contain 1–128 ASCII characters, start with a letter or digit, and subsequently
allow letters, digits, `.`, `_`, `:`, `-`. Principals are stable numeric
`github:user:<positive signed-64-bit ID>` identities, never aliases or role names.

The manifest is opaque bounded JSON in this library: its exact retained length
and digest must match. The caller independently authenticates its scope,
creation time and producer set. This call does not validate its twenty-five
gates, source registrations or custody. The proposed manifest schema has no
creation timestamp; an independently authenticated creation fact is therefore
required rather than adding an undocumented wire field.

The supported runtime is Linux CPython with Python's `time.CLOCK_BOOTTIME` and
`time.clock_gettime` available. One shared private elapsed helper uses that
clock for context acquisition, current checks and request deadlines. It counts
host suspension; unsupported/unavailable clocks refuse with no monotonic or
wall-clock fallback. Readiness waits are capped at 0.05 seconds so a resumed
process rechecks its elapsed deadline promptly. UTC authentication remains an
external prerequisite. The trusted UTC sample is acquired at context
construction and ages by this suspend-aware elapsed time. Its acquisition anchor is immutable, remains across
repeated consumption and is preserved when replacing a dataclass context.
Do not rebuild a context from a stale timestamp as a way to refresh it; a new
UTC sample requires fresh authenticated external time. Contexts belong to
their acquisition clock/boot lifetime; transfer or restoration into a different
lifetime requires a newly authenticated time sample. Invalid or regressing
elapsed samples refuse before observations are returned. No local wall-clock
fallback, default approval lifetime or default role grant is provided. Current
role/session grant status and expiry must be checked externally for every call;
the policy object is not a reusable grant. The caller must also verify the
independent producer identities for every actual capture in the manifest.

## Exact submitted review body

Each review body is a plain JSON object without a Markdown fence or commentary.
Its exact fields are:

| Field | Required value |
| --- | --- |
| `schema` | `hexalith.access-telemetry.c1.github-bundle-decision/v1` |
| `role` | `operations` or `security`, each once in separate reviews |
| `decision` | `approve` |
| `manifest` | Exact Ref object: `path`, `sha256`, `byteLength` |
| `profileSha256` | Trusted context's exact profile SHA-256 |
| `workloadSha256` | Trusted context's exact workload SHA-256 |
| `targetSha256` | Trusted context's exact target SHA-256 |
| `sessionId` | Trusted context's exact session ID |
| `sourceCommit` | Trusted context's full evidence source commit |
| `policySha256` | Independently supplied current policy digest |
| `expiresAtUtc` | Canonical UTC `YYYY-MM-DDTHH:MM:SSZ` |

Unknown/missing fields, duplicate decoded keys, BOM, invalid UTF-8, unpaired
surrogates, noninteger JSON numbers and the existing wire budget violations
refuse. Ref path/digest/positive length use the unchanged wire contract. A body
string has at most 4096 decoded characters because the bounded API JSON snapshot
applies the existing string ceiling. Retain a Ref to the exact UTF-8 review body
bytes before consumption. SHA-256 covers those bytes, including whitespace,
newlines and Unicode representation, without reserialization. A changed body
refuses even if its parsed meaning remains the same.

## Pair checks and time boundaries

The two locators must have distinct review IDs on the same exact owner,
repository and PR. Both expected reviewers must occur in their respective
independently authorized current scoped role allowlist. A repeated reviewer
refuses except for the existing named-owner exception
`github:user:6775094`, which may hold both roles with separate review decisions.
Either reviewer's overlap with any authenticated producer refuses, including
the named owner. The existing label-only legacy consumer cannot use this rule.

Every call re-fetches both bound reviews when the preceding checks succeed.
An unavailable/invalid review refuses the entire pair; a partial observation is
not returned. The authenticated response must have the exact positive review
ID, exact `pull_request_url`, exact `commit_id`, `state: APPROVED`, and a
positive numeric `user.id` with `user.type: User`. Fixture metadata matches its
fixture origin; production metadata matches `https://api.github.com`.
The stable reviewer principal and exact UTF-8 body Ref must match their retained
bindings. Unknown, edited, dismissed, pending, deleted or missing bound
decisions refuse. Currentness here covers the two retained review IDs and the
independently authenticated current role grants supplied for this call. The
library does not evaluate later unbound PR reviews, review supersession for
merge eligibility, branch protection or the PR's merge approval state. An
operational supersession policy remains external; this contract does not
choose one.

`submitted_at` and `expiresAtUtc` require second-precision canonical UTC with
no fractional seconds or offset aliases. Submission must be at or after the
independently authenticated manifest creation and no later than the trusted
time at that request's start. Review lag may equal the explicit maximum.
Approval lifetime must be positive and may equal its explicit maximum. At the
final check, trusted time must be strictly before expiry; equality refuses.
Request latency and context age both count toward expiry. Status age is counted
conservatively from each request's start through validation of the entire pair,
including the second request. An age greater than the explicit maximum refuses.

## Calling the library

The caller imports `ApprovalContext`, `CurrentRolePolicy`, `ReviewBinding` and
`consume_bundle_reviews` from the Memories module; `ReviewLocator`,
`RequestLimits` and `GitHubReviewClient` are also exposed through that import.
From the repository root (or with that root on Python's import path), import
the module directly. The library resolves its trusted sibling tooling and exact
Platform source; callers do not need to add a separate tools search path.
Construct the required objects from independently authenticated inputs, then:

```python
from tools.access_telemetry_c1_github_approvals import GitHubReviewClient, consume_bundle_reviews

# All four variables below come from the approved external authority adapter
# and secret boundary, with the complete mandatory facts described above.
pair = consume_bundle_reviews(
    client=GitHubReviewClient(),
    token=secret_boundary_token,
    context=trusted_context,  # expected_issuer must be https://api.github.com
    operations=retained_operations_binding,
    security=retained_security_binding,
)
```

Executable Python callers must guard their main entry point with
`if __name__ == "__main__":` for the spawned request worker. Calls from a
daemon process or an environment that cannot spawn refuse. This is an importable
library, not a newly introduced operator command.

The returned tuple contains two frozen `ReviewObservation` values in Operations,
Security order. Each retains issuer, role, principal, locator, manifest Ref,
scope, source/PR commits, policy digest, exact response/body digests, submission,
expiry, status-request start and final checked UTC. Raw API/body bytes, bearer
credentials and grants are excluded. Observations describe this invocation;
they are not an accepting predecessor, a gate artifact or an action handle.
Downstream use requires a fresh call and all external prerequisites again.

## Fixture verification and publication handoff

The explicit `GitHubReviewClient(fixture_port, fixture_ca_pem)` can contact only
certificate- and hostname-verified `127.0.0.1`. Its issuer is visibly
`fixture:github-reviews:https://127.0.0.1:<port>`, never the production issuer.
A context expecting production refuses that client. Tests generate ephemeral
local certificates using the runner's `openssl` executable and exercise real
HTTPS, including denial, redirect, byte, TLS, stalled/trickled response and
deadline cases. A resolver-stall fixture proves DNS is inside the same deadline. Delayed
bootstrap with a maximum CA payload proves parameter transfer is interruptible
and leaves no orphan. Oversized inherited process names, import paths, arguments
and IPC authentication metadata refuse before that blocking bootstrap. Successful
connection-close responses at the exact byte ceiling, incomplete lengths,
truncated chunks, zero review lag, independent policy scope mismatches and
otherwise-valid malformed API envelopes have dedicated real TLS regressions.
Complete subprocess output is checked for credential
canaries. An isolated-DNS production-constructor transport test uses real local
TLS to verify the fixed hostname/443/system-context selection; it creates no
production review observations. Decision-time boundaries are controlled
independently of TLS scheduling, including suspend-like clock advancement.
Fixture success never supplies operational authority or real GitHub approval.

Run the focused and unchanged legacy lanes:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v
```

Missing Platform source fails discovery; it is never skipped. CI explicitly
initializes only the root-declared Platform submodule at its pinned commit,
without recursive or remote updates. The final reviewed Platform library, including these startup/keylog corrections,
must be published by Platform's owner, then Memories must separately advance
the root gitlink to that published commit. A separate workspace operation
advanced Platform to published commit
`7192c313edc39c6f698bc25c5475ff41d12197cd` during review; that commit contains
the earlier library, while the latest corrections remain working changes. Until those separately
authorized steps occur, a fresh pinned checkout cannot load the unpublished
library and is not shippable. The current turn preserves the root gitlink and
does not stage, commit, publish or register an accepting provider.

GitHub prevents a PR author from approving their own PR. Actual evidence PRs
require an independently authorized producer/PR author distinct from the
reviewer; this library does not create a bot, impersonate anyone or establish
that author authorization. See [GitHub's approval restrictions](https://docs.github.com/en/pull-requests/how-tos/review-pull-requests/approving-a-pull-request-with-required-reviews).
Sessions, authenticated producers, custody, trusted time, current role grants,
remaining approval topology and all accepting-consumer work still require their
separate evidence and decisions.
