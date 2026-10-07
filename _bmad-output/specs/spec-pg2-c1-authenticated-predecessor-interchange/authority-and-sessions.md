# Authority, session eligibility and decision validity

## Investigation result and integration boundary

The source mechanisms and their limits are recorded with reproducible commands
in [brownfield.md](brownfield.md). None of the inspected mechanisms implements
C1 reviewer authorization plus a retained, revocable decision receipt.
**No authentication scheme, issuer, key, role grant or deployed service is
selected by this spec.** P1 is a Security-owned blocking decision.

Consume the selected mechanism through one required authority adapter. Do not
build a new proprietary Memories MCP server/CLI or receipt issuer. If the
capability belongs in generic platform/operator infrastructure, its owning
technical module and approved McpCli transport must be selected before migration.
The Memories verifier consumes the approved contract and supplies no consent.

Authentication proves principal identity; authorization proves that principal
can perform the particular review/action on this gate, profile, target and
session. Both must be evidenced independently of workload authentication.

## Required authority-adapter facts

These are verified internal results, **not a claimed existing receipt schema**:

| Bound subject | Required verified facts |
| --- | --- |
| Producer execution | Stable opaque principal; actual capture hash; literal gate, approved source/producer/target/profile/workload/session; authorized capture action and collection interval. A later operator assertion cannot recover an unauthenticated historical execution. |
| Gate disposition | Trusted issuer and decision ID; exact capture bytes/hash/length; producer and reviewer principals; permitted review role/action; full gate/profile/workload/target/session scope; decision, reasons and independence; decision time and expiry; current unrevoked authority. |
| Session | Authentic grant ID, subject/principals, target cluster identity and namespace, profile/workload, permitted gates/actions and source/registry/policy revisions, collection/review/use windows, custody and cleanup scope; current grant status. |
| Bundle approvals | Trusted issuer and decision ID; exact manifest bytes/hash/length; approved role/principal and scope; decision/reasons/time/expiry; independence against the actual producer identities; current revocation status. |

Verify retained receipt bytes against their expected digest **and** the supported
issuer's authenticated mechanism and authorization policy. A valid signature
from an untrusted issuer, a receipt URL or a locally supplied hash cannot pass.
The adapter must reject substitution, ambiguous identity mapping, unknown role,
wrong audience/scope and replay across decisions or sessions. Bind disposition
fields to receipt facts rather than accepting a trustworthy issuer's unrelated
decision. Receipt wire format, algorithms, discovery, issuer policy and key
rotation/revocation are P1/P2 decisions.

Producer/reviewer identities in retained evidence must be opaque and content-free.
Credentials are transport inputs supplied through the approved secret boundary;
they do not appear in JSON, command arguments, logs, hashes of unsafe streams or
error output. A receipt must not be a reusable bearer credential.

## Session eligibility

Acceptance requires one explicitly authorized session, matching every capture,
disposition, manifest and approval. At validation time, prove all of:

1. The grant is authentic, unrevoked and currently valid for the requested use.
2. `PG-ONPREM-2`, canonical profile/workload hashes, permitted source revisions and registered producers match exactly.
3. Target cluster identity is authenticated; context/namespace/selector/app/actor labels alone do not establish it. Bind each capture's `targetSha256` to that grant's approved target and protected observation window.
4. Producer and reviewer actions occurred in their permitted windows and scopes; review follows the capture's finish, and aggregate review follows manifest creation.
5. All twenty-five gates belong to that eligible session. Do not join convenient captures from unrelated or expired sessions.
6. Custody supplies retrievable immutable bytes and required supporting evidence under the approved root; cleanup and the unchanged Production boundary are proved where applicable.

P3 must decide whether a qualification session can authorize read-only observation
of the current `jpiquot@local` / `hexalith-memories` C1 target. Existing C2-C4
producers require a different isolated namespace ending `-qualification` and
refuse `hexalith-memories`. Do not equate those scopes or use a C1 session as
permission to mutate either target.

The current C1.16 acceptance covers a closed capture window with independently
accepted full-set connection linkage and a preserved single-session refusal.
It supplies neither a new session nor authority to rerun. P6 must prove eligibility
from independently existing authentic authority and complete facts, or require
fresh versioned capture/review. Never backfill missing facts, erase the failed
canonical packet, or discard its historical acceptance.

## Independent time, expiry and revocation

| Check | Required behavior |
| --- | --- |
| Capture freshness | Enforce approved collection duration, permitted capture age and future-clock tolerance from P2, plus collection-window membership. Freshness does not establish authority. |
| Decision ordering | Capture finishes before disposition; `decidedAtUtc < expiresAtUtc`; permitted review lag, expiry and session review windows all hold. No indefinite acceptance or default expiry. |
| Authorization at assembly/use | Current time lies inside the approved use window and before the earliest applicable session, role grant, receipt or decision expiry. Expiry at the current instant refuses. |
| Revocation | Resolve current authenticated status for session, authority grant, decision and relevant trust keys at assembly and immediately before downstream use. Revoked or unknown status refuses even before expiry. |
| Status freshness | P2 fixes maximum revocation-status age and any approved offline status-proof protocol. No cached `unrevoked` result, fixture response or unreachable endpoint silently passes. |
| Time source | P2 selects an authenticated time basis and numerical skew policy. Local timestamp labels or unrelated lifecycle clock signatures cannot appoint review authority. Unavailable/untrusted time refuses. |
| Historical inspection | May report the original accepted decision and subsequent expired/revoked status. It must not return an execution-eligible handle or enable a target. |

The existing Python authorization path applies a 15-minute capture-finish ceiling
and one-second future skew. Preserve that stricter pre-launch check where it
applies until a separate consumer policy change is approved; it does not decide
the new twenty-five-gate session horizon, long-running gate durations, review
lag, approval lifetime or revocation freshness. No unspecified window defaults
to that fifteen minutes. P2 must reconcile the two policies explicitly.

Admission cannot promise authority will remain valid indefinitely. Downstream
consumers must revalidate at use; long-running actions require their owning
renewal/revocation/cleanup protocol. This slice never grants such actions or
starts them. A retained `qualificationAuthorized: true` cannot bypass revalidation.

## C1 approval-gate independence and cycles

C1.23 operations approval, C1.24 non-HA acknowledgement and C1.25 independent
security approval each need their own capture, authenticated decision,
registered producer and accepted artifact. They cannot all point to one approval
packet, and the two aggregate approvals cannot stand in for them.

P4 must specify the upstream evidence subset each approval gate decides on and
the principal-separation topology. Do not make a gate approve a manifest that
already contains that gate's own decision. The proposed manifest is frozen
after the twenty-five accepted artifacts; later bundle approvals bind it without
being included in its hash. Distinct role labels, receipt IDs or usernames that
resolve to one authority principal do not prove independence.

Owner decision, 2026-10-07: Jérôme Piquot explicitly chose to approve both
Operations and Security roles. The authenticated GitHub profile lookup identifies
`jpiquot`, stable account ID `6775094`; the policy principal is
`github:user:6775094`. Only that verified owner may supply both bundle review
roles. Each role still requires its own authorized, bound decision and distinct
receipt on the frozen manifest. The owner cannot approve any capture they
produced; alias usernames cannot hide that conflict. P4's remaining delegation,
quorum and approval-gate dependency questions remain unresolved. Reconsider this
reduced role separation before Production activation or an account, role or
producer-identity change. The pure separation predicate is implemented in
`tools/access_telemetry_c1_approval_policy.py`; it grants no authentication,
receipt validation or execution authority. The label-only legacy predecessor
cannot consume this exception and retains its existing denial behavior.
