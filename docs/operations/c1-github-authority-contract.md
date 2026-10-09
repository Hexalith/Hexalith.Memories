# C1 GitHub policy, session and review authority contract

This read-only prerequisite authenticates a root-approved policy, a delegated
session and exact gate or bundle reviews through fresh GitHub HTTPS responses.
It returns immutable checked observations. Capture execution provenance,
authenticated target identity, custody, manifest facts, semantic gate admission
and eligible downstream use still require independent evidence. No accepted
gate, predecessor, live grant, execution handle or deployment call is produced.
Story 27.4, A41, Production, historical evidence and legacy consumers retain
their existing state.

## Offline disposition consumption boundary (2026-10-09)

`tools/access_telemetry_c1_authority_boundary.py` exposes
`require_authenticated_disposition`. It checks a C1.15 retained capture's
SHA-256 and length, strict capture/disposition shape, exact capture Ref, gate,
current PG2 profile/workload, source commit, target and session labels, and an
observed clean capture with zero failures/skips. It rejects a review dated at
or before capture completion and requires an asserted accepted decision. It
then always raises
`authority-prerequisites-unapproved`. It performs no receipt retrieval, GitHub
request, target call or artifact publication. Its caller-supplied scope and
retained bytes do not authenticate execution, cluster identity or custody.

This boundary is deliberately terminal while I3 remains incomplete. The
structural `authorityReceipt` URI, digest, issuer and decision ID are claims,
not an approved trust root or receipt verifier. The existing GitHub chain
returns fixture-capable observations only and cannot bypass this refusal.
Security must approve P1's complete producer/gate/session identity and receipt
protocol, issuer, trust roots, retrieval, revocation and owning maintainer.
Security with Operations must approve P2's numerical windows, authenticated
time, status freshness and rotation policy. Operations with Security must
approve P3's actual target identity, eligible session grant and custody/cleanup
contract. Security, gate approvers, Operations and Architecture must finish
P4's gate role/delegation/quorum matrix and acyclic C1.23–C1.25 dependencies.
The owner-only two-bundle-role exception remains the sole partial P4 decision.

## Ownership and supported transport

Platform owns `eng/hexalith_github_decisions.py`, the application-neutral strict
JSON and exact-review/body authentication library. It uses the existing unchanged
`eng/hexalith_github_reviews.py` transport. Memories owns
`tools/access_telemetry_c1_github_authority.py` and its C1 schemas and rules. The
existing snapshot/Ref helpers, principal separation rule and bundle consumer
remain unchanged. No dependency, CLI, service, issuer, key or workflow is added.

The existing [review transport contract](c1-github-review-contract.md) applies:
production uses certificate-verified `api.github.com:443`, fixed validated review
paths, authenticated GET, no redirects/proxies/cached/offline fallback, explicit
byte/deadline limits and disposable deadline-bound workers. A loopback TLS
fixture has an explicit `fixture:github-reviews:https://127.0.0.1:<port>` issuer;
it cannot satisfy a production bootstrap root.

Generic authentication requires the exact API fields `id`, `user`, `state`,
`body`, `commit_id`, `submitted_at`, `pull_request_url`. Supported optional
GitHub metadata is `node_id`, `html_url`, `author_association`, `_links`;
unknown top-level fields refuse. Metadata supplies no permission. `user.type`
must be `User` and `user.id` a positive signed-64-bit integer matching the
retained canonical principal. Review ID, exact PR URL, selected PR commit,
`APPROVED` state, exact UTF-8 body length and SHA-256 must all match. Body bytes
are hashed directly, including whitespace; changed equivalent JSON refuses.
Generic observations freeze the body tree but know no C1 schema or role policy.

## Independent bootstrap and caller facts

`BootstrapRoot` is an independently authenticated configuration input. Creating
the object checks structure; it does not authenticate the operator or adopt an
operational policy. A policy cannot select its root. Every field is required:

| Field | Required independent fact |
| --- | --- |
| `expected_issuer` | Exact production issuer or explicitly marked fixture issuer. |
| `owner`, `repository` | Exact trusted evidence repository; every dependent review stays there. |
| `policy_reviewers` | Nonempty immutable set of stable `github:user:<numeric-id>` policy reviewers. |
| `trusted_now_utc`, `time_acquired_elapsed` | Authenticated aware UTC sample and matching Linux `CLOCK_BOOTTIME` acquisition anchor, from the same clock/boot lifetime. |
| `request_limits` | Explicit `RequestLimits(max_response_bytes, deadline_seconds)` for each fresh request. |
| `max_status_age_seconds` | Positive finite maximum age from request start through completion of the entire chain. |
| `max_decision_lifetime_seconds` | Positive finite maximum issued-to-expiry lifetime for every decision. |
| `max_review_lag_seconds` | Positive finite maximum issued-to-submission lag, and capture-finish-to-gate-review lag. |
| `max_dependencies` | Explicit integer from 2 through 100, counting policy, session, gate parents and both bundle reviews. |

`AuthorityScope(profile_sha256, workload_sha256, target_sha256, tenant_id,
session_id, source_commit)` supplies the independently authenticated expected
scope. This adapter requires these exact published current identities:

| Identity | SHA-256 |
| --- | --- |
| `PG-ONPREM-2` profile | `7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe` |
| Approved two-writer workload | `71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f` |

Historical or inexact profile/workload hashes refuse at construction; arbitrary
hashes cannot acquire a PG2 label. Target SHA-256 remains an independently
authenticated actual target identity, not a hash of labels. Tenant and session
IDs are 1–128 ASCII characters, start with a letter/digit and subsequently allow
letters/digits/`.`/`_`/`:`/`-`. Digests are lowercase 64-digit hexadecimal;
source/PR commits are lowercase full 40-digit hexadecimal.

`AuthorityBinding(review, reviewed_commit)` wraps the existing `ReviewBinding`:
exact locator, expected principal, body Ref and immutable retained bytes. Each
policy/session/gate can select a distinct PR and exact commit in the trusted
repository. A selected review ID cannot serve two dependencies. The bundle
retains two distinct IDs on one exact PR and its independently selected commit.
`AuthorityContext(root, scope, policy, session)` carries these unverified
retained inputs; previously returned observations cannot replace them.

`GateFacts` requires a literal gate, exact capture Ref/immutable JSON bytes,
independently authenticated capture finish UTC and actual producer principals.
`ManifestFacts` requires exact manifest Ref/immutable bytes, separately
authenticated manifest creation UTC, every actual capture producer principal
and the selected bundle PR commit. These objects do not authenticate their own
assertions. The collector must verify actual execution/source/target/session
provenance and custody before supplying them. A session's planned producer list
or a later reviewer assertion cannot repair missing historical execution facts.

## Closed machine body schemas

Each submitted body is a plain UTF-8 JSON object. Field sets below are exact
unions: no unknown/missing fields, Markdown fences or comments. References use
the unchanged exact Ref shape `{ "path": ..., "sha256": ..., "byteLength": ... }`.
Paths are safe relative canonical paths; positive length and SHA-256 match the
retained bytes. Duplicate decoded keys, BOM, invalid UTF-8, unpaired surrogates,
noninteger/out-of-range numbers and depth/string/byte budget violations refuse.
The generic JSON ceilings are 1 MiB, depth 14 and 4096 characters per string;
the API's decoded review body therefore also has a 4096-character ceiling.
The request byte limit may be stricter. Arrays below contain no duplicate values.

All three schemas include exactly these common fields:

| Field | Value/type |
| --- | --- |
| `schema` | One exact supported schema string below. |
| `decision` | `approve`. Withdrawal is detected through current review state, changed bytes or unavailable status. |
| `profileId` | `PG-ONPREM-2`. |
| `profileSha256`, `workloadSha256` | Exact published identities and expected scope. |
| `targetSha256`, `tenantId`, `sessionId`, `sourceCommit` | Exact independently expected scope. |
| `issuedAtUtc`, `expiresAtUtc` | Canonical second-precision UTC `YYYY-MM-DDTHH:MM:SSZ`. |

The policy uses schema `hexalith.access-telemetry.c1.github-policy/v1` and adds
exactly:

| Field | Value/type |
| --- | --- |
| `sessionReviewers` | 1–100 distinct canonical principals permitted to approve this scoped session. |
| `reviewerGrants` | 1–100 exact grant objects described below, with one entry per principal. |
| `permittedGates` | 1–25 distinct literal `C1.1` through `C1.25` values. |
| `permittedActions` | Nonempty subset of `review-gate`, `review-bundle`. No capture or deployment action is supported. |
| `gateDependencies` | Exact object with one field for every permitted gate; each value is an array of permitted parent gate names, which may be empty. The directed graph must be acyclic. |
| `maxStatusAgeSeconds`, `maxDecisionLifetimeSeconds`, `maxReviewLagSeconds` | Explicit positive integer limits no greater than the independent bootstrap bounds. |

Policy cannot appoint a policy reviewer or replace the bootstrap issuer,
repository or time source. Its independently retained reviewer must already be
allowed by the root. The fixture dependency matrix is illustrative; it adopts
no operational C1.23/C1.24/C1.25 approval topology.

The session uses schema `hexalith.access-telemetry.c1.github-session/v1` and
adds exactly:

| Field | Value/type |
| --- | --- |
| `policy` | Exact Ref to the authenticated policy body. |
| `reviewerGrants`, `permittedGates`, `permittedActions` | Nonempty subsets of that policy's permissions; each grant principal and each gate/action/role must already be allowed. |
| `producerPrincipals` | 1–100 distinct planned/allowed producers. Every independently authenticated actual producer must be present. This field supplies no execution provenance. |
| `notBeforeUtc`, `collectionEndsAtUtc`, `reviewEndsAtUtc` | Canonical UTC window boundaries, satisfying `issued <= notBefore < collectionEnd <= reviewEnd <= expiry`. |

Every grant object has exactly `principal`, `gates`, `actions`, `roles`.
`principal` is canonical; `gates` is a nonempty permitted gate array; `actions`
is a nonempty permitted action array; `roles` is a nonempty subset of
`operations`, `security`. Duplicate principals, unknown roles/actions or any
session escalation refuse. Review grants cannot overlap planned producers;
policy, session, gate and bundle reviewers cannot overlap any supplied actual
producer. Author association, login aliases and display names never grant roles.

The gate review uses schema
`hexalith.access-telemetry.c1.github-gate-review/v1` and adds exactly:

| Field | Value/type |
| --- | --- |
| `policy`, `session` | Exact body Refs to the authenticated parents. |
| `gate`, `role` | Exact independently requested gate and a permitted Operations/Security role. |
| `capture` | Exact Ref matching the independently supplied retained actual capture bytes. |
| `parents` | Array of exact gate-review body Refs. The parent gate set must equal the authenticated policy's dependency matrix for this gate; no duplicates, missing nodes, cycles or mixed scopes. |

One invocation supplies at most 25 unique gate requests. Parents are validated
and re-fetched before their children regardless of input tuple order. Gate
review submission must follow actual capture finish, session approval and every
parent submission. Actual capture finish lies in the collection window and
submission lies in the review window. Finish-to-review lag is independently
bounded; issued-to-review lag cannot hide an old capture.

Bundle bodies remain the existing unchanged
`hexalith.access-telemetry.c1.github-bundle-decision/v1` contract. Policy digest,
manifest, source, profile, workload, target and session ID bind exactly. The existing bundle body has no exact session-body Ref. The separately
returned session ancestor observation retains the authenticated grant's exact
body Ref, but does not prove that the bundle reviewers approved that particular
grant revision. Before collector adoption, independently verify one immutable
grant Ref per session ID and reject ambiguous or reused IDs, including a
replacement grant using an existing ID. Never infer approval of replacement
grants from older bundle decisions. This mapping is an external collector
prerequisite; the adapter supplies read-only observations and preserves the
existing bundle schema. Tenant and
PG2 identity are checked through the authenticated ancestor scope. Both roles
derive from the verified session grants. The named owner
`github:user:6775094` may occupy both roles with separate decisions; no other
same-principal pair passes, and the owner cannot overlap a producer.

## Withdrawal, clocks and whole-chain revalidation

Every call fetches policy, session, all supplied gate dependencies and both
bundle reviews through the same bounded authenticated transport. Edited,
dismissed, pending, missing, denied, redirected or unreachable bound reviews
refuse the entire result. No partial tuple escapes, and no target is contacted.
This checks current state of the retained exact review IDs; unbound later PR
reviews, merge eligibility and branch-protection supersession remain outside
the contract and require an independently chosen operational policy.

Policy submission precedes session issue; session approval precedes the
collection start. Every issue follows the relevant authenticated parents;
submission follows issue and precedes expiry, and cannot be after trusted UTC
at the request start. Decision lifetime and review lag must satisfy their
explicit bounds. The current use time lies in the session review window.
Approval expiry at the current instant refuses. Review-lag/lifetime/status-age
maxima are inclusive; ordered window expiry/use boundaries are exclusive.

Trusted UTC advances from its immutable acquisition anchor using the existing
suspend-aware Linux clock, including host suspension and all request latency.
Invalid, unavailable or regressing elapsed readings refuse with no wall-clock
fallback. Reusing or replacing a context preserves its acquisition anchor; a
new anchor requires a newly authenticated independent UTC sample. Restoring or
transferring across a boot/clock lifetime requires reacquisition.

Status age starts at each dependency's request start. The adapter checks the
entire authenticated chain after each new review and again after bundle
retrieval. Earliest policy/session/gate/bundle expiry and session review-window
end constrain the result. A fresh bundle status cannot renew an earlier policy
status or expired grant. Returned `AuthorityChainObservation` contains frozen
policy/session/gate facts, optional frozen bundle observations, effective
earliest expiry and final check UTC; no mutable policy, token or executing handle
is returned. Reuse for a later decision requires a new complete consumption.

## Fixture and API examples

These commands run genuine loopback TLS fixture chains with ephemeral
certificates. Fixture numerical values and identities appoint no live policy:

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_github_authority.py' -k test_real_tls_root_policy_session_parent_gate_and_owner_bundle_chain -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_github_authority.py' -k test_gate_only_tls_chain_topological_order_and_no_bundle_acceptance -v
```

The executable fixture test constructs every mandatory input and invokes these
APIs directly. A collector with independently authenticated inputs uses the
same calls; its main entry point must be guarded for the spawned worker:

```python
from tools.access_telemetry_c1_github_authority import (
    GitHubReviewClient, consume_authorized_bundle_reviews, consume_gate_reviews,
)

def observe(credential, authority_context, capture_requests, manifest_facts,
            retained_operations_review, retained_security_review):
    client = GitHubReviewClient()
    gate_facts = consume_gate_reviews(
        client=client, token=credential, context=authority_context,
        requests=capture_requests,
    )
    bundle_facts = consume_authorized_bundle_reviews(
        client=client, token=credential, context=authority_context,
        manifest=manifest_facts, operations=retained_operations_review,
        security=retained_security_review, gate_reviews=capture_requests,
    )
    return gate_facts, bundle_facts

# The caller's executable uses if __name__ == "__main__": and obtains these
# inputs through its independently approved trust/time/provenance/secret paths.
```

Imports resolve exact pinned workspace Platform and Memories approval,
principal-policy and wire files. Missing or unrelated cached/installed modules
fail import; both file and import-origin metadata must match the exact workspace
source. No permissive fallback or skipped
provider verification is supported. The independent test lane includes tenant,
target, session, action and role denials before dependent requests, exact-body/
commit/user withdrawal cases, malformed envelopes, context reuse, earliest
expiry and status ageing across real TLS bundle calls. Success verdicts are
never mocked. Clock boundary tests control time independently of TLS scheduling.

## Verification and collector handoff

```bash
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p 'test_*.py' -v
env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p 'test_*.py' -v
git diff --check
```

Executed counts, command output and source identities are recorded in the
implementation spec and `/tmp/pg2-c1-authority-lbzyqupl`. Platform publication
and a separately authorized matching Memories gitlink advance remain necessary
for a fresh pinned checkout; this implementation makes neither Git mutation.
An AppHost baseline attempt was blocked by the missing nested Builds import;
no nested checkout was initialized and the Python library is verified separately.

Before actual collector adoption, owners still must supply an operational
bootstrap root, approved policy/session/window/dependency matrix, authentic
eligible capture provenance, target identity and custody. Implemented gate
producers/semantic verifiers/registrations and accepting predecessor consumers
remain separate prerequisites. GitHub's own author-review restrictions still
apply; the library creates no review, PR, bot or credential. It does not infer
execution permission from an authenticated review or close P1–P7, Story 27.4,
A41 or the Production hold.
