---
title: 'PG-ONPREM-2 C1 P1 read-only receipt retrieval and status'
type: 'feature'
created: '2026-10-09'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md'
  - '_bmad-output/implementation-artifacts/spec-pg2-c1-p1-enrollment-receipt-transport.md'
  - 'references/Hexalith.Platform/docs/contracts/p1-receipt-provider-v1.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** P1 has no authenticated HTTP retrieval path, fresh status request, retained response bytes or immutable-ID comparison.

**Approach:** Add a read-only Platform Identity HTTP adapter using enrolled origins, strict TLS/mTLS, bounded GETs, generated status challenges and the offline verifier. Return copied exact bytes; test only against loopback TLS/mTLS fixtures.

## Boundaries & Constraints

**Always:** Require `P1AuthenticatedEnrollment`, independently pinned server anchor/hostname and scoped mTLS client issuer, chain, trust domain and SPIFFE URI SAN, plus expected claims, subject bytes and authenticated UTC at verification. Use exact credential-free locators; generate a fresh random 32-byte lowercase hex nonce per status GET. Enforce 10 seconds per request and 300 seconds for the monotonic chain from first request start. Require HTTP 200, one exact media type, complete untransformed body <=1,048,576 bytes, and canonical framing/signature/status checks. Disable redirects, proxies, cookies, decompression and client caching; request no-store and reject cached responses. Compare full framed receipt bytes, including signature, with caller-supplied prior observation and IDs seen by this adapter instance; classify mismatch as an incident. Return defensive copies of receipt/status/subject bytes bound to `P1VerifiedReceiptEvidence`.

**Never:** Bypass certificate validation, treat fixture pins as operational, contact a live issuer/target during this work, publish a bearer receipt, register an accepting consumer, advance I3/27.4/A41, or enable Production writes. The result grants no authority or custody proof.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Local verified chain | Loopback TLS/mTLS, enrolled origins/keys, exact receipt and fresh status | Return copied bytes and evidence from one verification snapshot | Fixture authority remains local |
| Transport refusal | Wrong identity/origin, redirect/cache/transform, wrong or duplicate type, bad framing, oversize or timeout | No observation | Refuse without fallback |
| Authority refusal | Replayed/stale nonce, revoked state, wrong claims/signature, missing trusted time | No observation | Fail closed |
| Immutable ID | Valid but changed framed receipt under prior observed ID | No observation; classify as incident | Never silently replace prior bytes |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1AuthenticatedEnrollment.cs`, `P1BootstrapVerifier.cs` — use root-pinned enrollment and canonical origins.
- `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptTransportV1.cs`, `P1SignedEnvelopeV1.cs`, `P1ReceiptVerifier.cs` — reuse decoder/verifier; retain framed bytes separately.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptHttpAdapter.cs` and one-type-per-file policy/result helpers — implement strict GET chain, TLS/mTLS, nonce, limits, ID comparison and copied observation.
- [ ] `references/Hexalith.Platform/tests/Hexalith.Platform.Identity.Tests/P1ReceiptHttpAdapterTests.cs` and fixture helpers — prove each matrix row with loopback TLS/mTLS.
- [ ] `references/Hexalith.Platform/docs/contracts/p1-receipt-provider-v1.md` — list independent Security/Operations inputs and fixture, clock and custody limits.
- [ ] Memories readiness files — inspect holds without changing them.

**Acceptance Criteria:**
- Given local fixture facts and supplied scope/time, when both GETs complete, then exact copied bytes and evidence agree, the nonce is fresh, and no write request is sent.
- Given any transport, identity, authority, deadline or immutable-ID failure, when retrieval runs, then it returns no positive observation and does not follow another origin.
- Given only this prerequisite, when readiness is inspected, then I3 still refuses, Story 27.4 remains `RECHECK_ONLY`, A41 stays open and Production writes stay disabled.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

An instance remembers observed IDs; a caller can pass a prior retained observation across instances. Durable history needs authenticated external custody. Production TLS requires hostname/system chain validation plus enrolled anchor; test-only loopback roots still require chain and hostname checks.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Platform.Identity.Tests/Hexalith.Platform.Identity.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/memories/references/Hexalith.EventStore -p:NuGetAudit=false -m:1` from `references/Hexalith.Platform` — expected: zero errors.
- `dotnet tests/Hexalith.Platform.Identity.Tests/bin/Debug/net10.0/Hexalith.Platform.Identity.Tests.dll` from `references/Hexalith.Platform` — expected: all Identity tests pass.
