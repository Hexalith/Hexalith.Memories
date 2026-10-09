---
title: 'PG2 C1 P1 authenticated bootstrap and receipt verification'
type: 'feature'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '858a828d7cdb648d18f07a4a568e8d8a52f92113'
context:
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md'
  - '_bmad-output/implementation-artifacts/spec-pg2-c1-i3-offline-authority-boundary.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Platform P1 receipt verifier checks offline signatures and claims, but its enrollment is a caller-created record. No independently authenticated bootstrap binds the issuer, keys and endpoints, so its positive fixture verdict cannot establish operational authority.

**Approach:** Add a canonical, signed bootstrap and root-pinned offline verification path in `Hexalith.Platform.Identity`. Require that path for receipt verification, and record the operator inputs still absent. Keep the Memories I3 consumer terminally refusing.

## Boundaries & Constraints

**Always:** Treat the P1–P4 owner dispositions as policy directions only. The root key identity must arrive through an independent operator trust channel; neither the signed bootstrap nor a receipt may appoint its own root. Bind exact bootstrap revision/digest, issuer, audience, receipt and separate status keys, HTTPS origins, validity and principal-mapping revision. Verify canonical bytes and signatures, exact subject bytes/length/hash, all expected claims including decision/reasons, fresh nonce-bound status and exclusive expiries. Missing, revoked, unknown, stale or unavailable facts refuse.

**Never:** Embed fixture keys or `NO_*` values as operational defaults; contact a live target; add an accepting I3 path, deployed C1 registration or execution grant; alter Story 27.4 `RECHECK_ONLY`, C0–C6 pending, disabled Production writes or open A41. Do not imply that a signed assertion proves actual historical execution, target identity or custody.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Offline fixture | Independently pinned test root, exact signed bootstrap, receipt, subject and fresh status | Typed authenticated enrollment permits exact P1 fixture verification | Fixture result grants no operational authority |
| Bootstrap attack | Missing/wrong root, altered bytes, untrusted issuer/key/endpoint, expired revision | No enrollment or receipt result | Refuse without fallback |
| Receipt/status attack | Forged or changed signature, subject/scope/reasons, nonce replay, revoked/unknown/unavailable status, expiry equality | No verified receipt | Refuse without target or network call |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptEnrollment.cs` — caller-created data today; do not treat construction as authentication.
- `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptWireV1.cs`, `P1ReceiptVerifier.cs` — reuse canonical encoding and P-256 exact-claim/status checks; require an authenticated enrollment result and refuse malformed null fields without exceptions.
- `references/Hexalith.Platform/docs/contracts/p1-receipt-provider-v1.md` — existing offline wire contract and explicit `NO_*` operational register; extend with bootstrap wire/root procedure and remaining inputs.
- `references/Hexalith.Platform/tests/Hexalith.Platform.Identity.Tests/P1ReceiptVerifierTests.cs` — preserve existing four-kind fixture tests and add bootstrap and fail-closed cases.
- `tools/access_telemetry_c1_authority_boundary.py` — terminal I3 refusal; leave unchanged.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Platform/src/Hexalith.Platform.Identity/` — add one-type-per-file bootstrap claims, canonical wire and root-pinned verifier, returning an authenticated enrollment only after exact signature, digest, scope and time checks.
- [x] `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptVerifier.cs` — require authenticated enrollment and preserve exact receipt/status/subject verification; malformed or missing inputs return refusal.
- [x] `references/Hexalith.Platform/tests/Hexalith.Platform.Identity.Tests/` — exercise a valid isolated signer and negative bootstrap, receipt, status, replay, scope, revoked, unavailable and expiry boundaries offline.
- [x] `references/Hexalith.Platform/docs/contracts/p1-receipt-provider-v1.md` — specify byte-level bootstrap/root contract and enumerate operator-supplied root identity, issuer, keys, retrieval/status origins and other operational dependencies still denied.

**Acceptance Criteria:**
- Given an independently pinned fixture root, when exact bootstrap, receipt, subject and current status are verified, then the provider returns only an offline authenticated observation.
- Given any missing, substituted, altered, expired or revoked trust input, when verification runs, then it refuses with no live target/dependency call.
- Given this prerequisite alone, when Story 27.4 readiness is inspected, then I3 and C0–C6 remain pending, Production writes remain disabled and A41 remains open.

## Implementation Notes

The Platform Identity module now has canonical signed bootstrap claims and wire encoding, an independently pinned P-256 root verifier, and a typed immutable enrollment result. Receipt verification accepts only that result. The verifier checks exact payload digest/revision, issuer, audience, origins, distinct receipt/status keys, principal-map revision and exclusive UTC validity. Test keys are isolated fixtures; the API does not enroll an operational root or contact an issuer, status service or target. The Memories I3 refusal and Story 27.4 protected paths have no diff.

The operator must still supply an independently authenticated root SPKI/fingerprint and current inventory, immutable bootstrap bytes/digest/revision, issuer, receipt/status keys and fingerprints, retrieval/status HTTPS origins, TLS/client credential anchors, principal mapping, authenticated UTC/status source, actual target/session grant and custody identities. The P1–P4 decisions grant none of these. P5–P7 and I3–I6 remain separate holds.

Review corrections bound decoding and signing to one snapshot of caller-owned bytes, reject oversized payloads before hashing or allocating field bytes, and reject non-UTC or submillisecond bootstrap times. Direct negative cases now cover malformed wire input, each independent origin/key pin, root-key role separation and exact expiry. The contract documents the no-rounding rule.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and evidence |
| --- | --- |
| Blind: hash before size check | low, patch: `TryVerify` hashes `document.Payload` before `TryDecode` rejects payloads above 1 MiB; a caller can spend avoidable CPU on oversized bytes. |
| Blind: encode allocates before size check | medium, patch: `Encode` calls `GetBytes` on a whole field before checking the one MiB cap, allowing a second large allocation before refusal. |
| Blind: submillisecond time truncation | medium, patch: `Time` formats with `fff`, silently moving a positive submillisecond `EffectiveAtUtc` earlier in the signed bytes. Reject nonrepresentable instants at encoding. |
| Blind: revoked root/status inventory absent | false: this offline API yields no operational grant, and the contract explicitly requires a current independently authenticated inventory before use; the owner asked to identify that still-missing operator input. Signed receipt/key/grant/policy/session states are checked here. |
| Blind: no serialized bootstrap envelope/media type | false: the offline API consumes the exact payload and detached signature as `P1SignedDocument`; an operator transport is not implemented or claimed, and the signed bytes and algorithm are fully specified for this prerequisite. |
| Blind: no independent wire vector | medium, patch: the fixture signs bytes from the same encoder that the verifier parses, so a shared encoding error could pass; add an independently constructed canonical byte vector. |
| Blind: malformed wire cases untested | low, patch: new decoder and root verifier have guards for length, UTF-8, SPKI and signature length, but the focused tests do not exercise those refusal paths. |
| Blind: fixed nonce fixture | false: challenge generation belongs to a future retrieval caller; the pure verifier compares the supplied nonce with signed status, and the mismatched-nonce test proves replay under a different challenge refuses. |
| Blind: incomplete independent pin tests | low, patch: current tests omit status-origin and receipt-key-fingerprint mismatch; those comparisons could be removed without a test failure. |
| Edge: oversized hash | low, carried with the first blind finding: the same missing prehash length guard causes the cited CPU cost. |
| Edge: mutable input race | high, patch: `TryVerify` hashes, parses and verifies caller-owned arrays at different times; concurrent mutation could make those checks observe different payload/root bytes. Snapshot them once. |
| Gap: status-origin and receipt-key pins | low, carried with the independent-pin finding: the cited comparisons lack direct refusal tests; add both. |
| Gap: root-key role separation | low, patch: the verifier rejects root reuse for receipt/status roles, but tests only cover receipt=status; add each root-reuse case. |

## Design Notes

No irreversible action is required. The footprint is new Platform Identity API types, its existing verifier, tests and contract documentation. Root enrollment itself remains an operator action outside this offline change; the new API must not equate a caller-supplied fingerprint with operational enrollment. The existing `PlatformBootstrapVerifier` is for a different identity-admission flow.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Platform.Identity.Tests/Hexalith.Platform.Identity.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/memories/references/Hexalith.EventStore -p:NuGetAudit=false -m:1` from `references/Hexalith.Platform` — exit 0, zero warnings/errors.
- `dotnet tests/Hexalith.Platform.Identity.Tests/bin/Debug/net10.0/Hexalith.Platform.Identity.Tests.dll` from `references/Hexalith.Platform` — exit 0; 21 total, zero errors/failures/skips/not run after review fixes. The three I/O matrix rows are covered by the four-kind positive test and bootstrap/receipt/status refusal tests.
- `dotnet test tests/Hexalith.Platform.Identity.Tests/Hexalith.Platform.Identity.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/memories/references/Hexalith.EventStore -p:NuGetAudit=false -m:1` from `references/Hexalith.Platform` — exit 1: Microsoft.Testing.Platform 2.4.0 rejects the VSTest target on .NET 10; direct xUnit assembly run above passed.
- `git -c core.whitespace=cr-at-eol diff --check` in `references/Hexalith.Platform` and `git diff --check` in the superproject — exit 0. The CRLF option matches the tracked `.gitattributes` policy.
