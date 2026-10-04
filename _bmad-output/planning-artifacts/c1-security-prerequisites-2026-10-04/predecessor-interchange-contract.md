# D3 Predecessor Interchange Preparation

Status: proposed implementation contract, source-audited 2026-10-04. No assembler,
new schema, approval authority or accepted predecessor is implemented here.

The current neutral `hexalith.access-telemetry.c1.evidence/v1` packets preserve
observations and grant no gate credit. They lack fields that the downstream C1
contract needs: canonical profile hash, full source commit, complete command
arguments/timings/exit status/nonzero result counts, qualification session and
independently authenticated dispositions. Those missing facts cannot be recovered
losslessly from old packets. Retain them as historical captures; require fresh
versioned captures for the successor profile rather than inventing fields.

## Current Validator Limits

`tools/verify_access_telemetry_lifecycle.py::_validate_predecessor` verifies 25
unique gate records, exact profile, artifact bytes, canonical Git source hashes,
command ledgers and gate freshness when requested. It does not parse the referenced
artifact's contents or restrict its source path to a registered producer. Its two
approval records contain only role, reviewer, state and profile hash. Different
reviewer strings do not authenticate authority, independence, a decision receipt
or approval freshness. These are implementation gaps, not existing guarantees.

## Required Artifacts and Binding

| Artifact | Required facts | Authority |
| --- | --- | --- |
| Fresh capture | Version/schema, exact gate/profile/workload/session/target; full source commit and all producer/helper hashes; actual process arguments, intervals, exit status, safe stream hashes and nonzero results; observations, failure/skip counts and cleanup receipts where applicable | Registered producer reads actual inputs. It cannot emit acceptance. |
| Independent disposition | Exact capture path/hash, gate/profile/session, decision and reasons; reviewer identity/role, independence from producer, decision time/expiry and authenticated authority receipt | Authorized independent reviewer evaluates the complete capture. No default pass. |
| Accepted per-gate artifact | Retained capture plus disposition references and hashes; validator-visible passing observations and exact source/command provenance | Reviewed verifier checks both artifacts before constructing the canonical gate record. |
| Canonical C1 predecessor | All25 distinct accepted gates on one eligible profile/session; actual Operations/Security decisions; disabled Production and current scoped qualification authority | Assembler validates the closed registry and genuine bound decisions. It creates no consent. |

Define the exact field sets, canonical JSON rules, byte limits, timestamp windows
and permitted enum values before code. Reject unknown/duplicate fields, invalid
types, empty results, symlink/path escapes and malformed/secret-bearing decoded
content. One gate cannot reuse another gate's artifact path or hash.

The closed producer registry must map each literal gate to its implemented
source path, helper set, schema, verifier and registered story. A valid Git blob
from an unrelated file is insufficient. Git hashes identify code, not authority.
The verifier must read each referenced artifact and compare its embedded
gate/profile/session/source/command facts against the record being assembled.

The authority contract must bind a retrievable authenticated decision to a
reviewer authorized for the actual role, target, profile and session. Select the
repository's supported authentication/receipt mechanism in the owning security
slice; no cryptographic or platform credential scheme is claimed here. Approval
time/expiry and revocation must be checked independently of capture freshness.
Operations and Security must be different authorized identities, and neither may
self-approve its own capture. C1.23/C1.24/C1.25 remain separately attributable.

## Required Negative Evidence Before Registration

The focused assembler/verifier fixtures must reject missing gates, duplicate
paths/hashes, altered artifact bytes, arbitrary registered-looking source paths,
abbreviated/unrelated commits, missing helpers, wrong command hashes, zero/skipped
results, mixed profiles, stale or unrelated sessions, contradictory payload fields,
unreviewed captures, forged reviewer labels, self-approval, expired/revoked authority
and missing cleanup. They must demonstrate rejection before any downstream
qualification dependency call. Genuine complete input must pass with nonzero
results; fixtures create no operational acceptance.

## Ownership and Reopen Conditions

Deployment Adapter Developer owns fresh capture/verifier/registry implementation;
Security owns the authority/receipt contract; Platform Operations owns target,
session, archive and cleanup inputs. Downstream C2-C4 authorization remains held
until exact successor adoption, supported producers, genuine decisions and this
reviewed executable interchange exist. Story 27.4 and A41 history stay unchanged.
