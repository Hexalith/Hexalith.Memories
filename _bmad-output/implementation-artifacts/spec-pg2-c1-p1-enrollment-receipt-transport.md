---
title: 'PG2 C1 P1 offline enrollment and receipt transport boundary'
type: 'feature'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'a053da5271f32c792932401c4ae0ef0a1495b9b8'
context:
  - '_bmad-output/specs/spec-pg2-c1-authenticated-predecessor-interchange/p1-p4-security-operations-decision-packet.md'
  - '_bmad-output/implementation-artifacts/spec-pg2-c1-p1-authenticated-bootstrap-and-receipts.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** P1 has a root-pinned offline bootstrap and exact signed receipt/status verifier, but it has no strict response-envelope reader and returns only a bool, so later code cannot bind its verdict to retained bytes. The documented 1 MiB response limit conflicts with a 1 MiB payload plus its 68-byte envelope.

**Approach:** Finish the offline transport boundary with exact bounded envelope and media-type decoding and an immutable verification-evidence result. Record the precise independent operator enrollment and live transport inputs still absent; do not create a live network or accepting consumer path.

## Boundaries & Constraints

**Always:** Keep the approved total HTTP response cap at 1,048,576 bytes, hence payload at most 1,048,508 bytes in that transport. Reject truncated, extra, malformed, wrong-type and oversized responses. Bind a positive offline observation to exact receipt/status payload and signature digests, subject digest/length, bootstrap digest, URI and nonce from the same verification snapshot. Keep independently supplied pins and trusted UTC mandatory; a pin supplied to the verifier is not proof of operator adoption.

**Never:** Enroll repository or fixture keys, invent a root/status service, contact a live target or issuer, expose a bearer receipt, accept Memories I3, alter Story 27.4 `RECHECK_ONLY`, close A41, enable Production writes or register an accepting C1 entry.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Offline fixture | Canonical framed receipt/status bodies, exact media types, pinned test root and current signed status | Decode and return immutable digests from exact verified snapshots | Observation grants no operational authority |
| Transport substitution | Wrong type, length, trailing bytes, payload over transport cap, missing or changed signature | No decoded document or verified evidence | Refuse without fallback |
| Retention mutation | Caller changes source arrays after verification | Returned digests remain tied to the verified snapshots | Altered retained bytes fail later digest comparison |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1BootstrapVerifier.cs` — creates typed offline enrollment from independently supplied pins; preserve its root boundary.
- `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptVerifier.cs` — snapshots signed documents and subject; add evidence result at this snapshot point, retain bool API.
- `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1SignedDocument.cs` — mutable input representation; decoder must return copies and result must contain no caller-owned arrays.
- `references/Hexalith.Platform/docs/contracts/p1-receipt-provider-v1.md` — defined envelope, media types, URI grammar, 1 MiB response limit and missing operator inputs; clarify transportable payload bound and adoption checklist.
- `references/Hexalith.Platform/tests/Hexalith.Platform.Identity.Tests/P1ReceiptVerifierTests.cs` — ephemeral signing fixture and focused verifier tests; extend for evidence result.
- `tools/access_telemetry_c1_authority_boundary.py` and Story 27.4/A41/Production files — inspect hold states; do not change.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1SignedEnvelopeV1.cs` — implement exact four-byte length + payload + 64-byte signature framing with a total 1 MiB cap.
- [x] `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptTransportV1.cs` — validate exact receipt/status media types and decode bounded bodies without network access.
- [x] `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptVerifier.cs` and new `P1VerifiedReceiptEvidence.cs` — emit immutable exact-byte digest evidence on success, preserving bool behavior.
- [x] `references/Hexalith.Platform/tests/Hexalith.Platform.Identity.Tests/` — cover framing boundaries, media types, signed fixture composition, and mutation after verification.
- [x] `references/Hexalith.Platform/docs/contracts/p1-receipt-provider-v1.md` — reconcile response/payload caps, explain the offline evidence shape and list exact independently authenticated operator inputs for live enrollment/transport.

**Acceptance Criteria:**
- Given an isolated signed fixture, when the exact framed responses are decoded and verified with an independently pinned test root, then the result identifies the exact verified bytes and remains an offline observation.
- Given any missing, substituted, malformed, oversized or changed transport bytes, when parsing or verification runs, then it refuses without a live dependency call.
- Given this prerequisite, when readiness is inspected, then I3 still refuses, Story 27.4 remains `RECHECK_ONLY`, A41 remains open and Production writes remain disabled.

## Implementation Notes

Platform Identity now decodes the exact length-prefixed detached-signature envelope and rejects bodies over 1 MiB, malformed lengths and wrong receipt/status media types. The decoder returns copied byte arrays. `P1ReceiptVerifier.TryVerify` returns an immutable observation containing SHA-256 digests of the same receipt/status payload and signature snapshots, the subject digest/length, verified bootstrap digest, exact locators and status nonce. The original `Verify` delegates to it. No HTTP client, operator enrollment, root inventory or Memories accepting path was added.

The Platform contract now states the transport payload maximum of 1,048,508 bytes while retaining the approved total-body cap. It lists the independent root, current inventory, TLS/mTLS, principal-map, time/status, target, grant and custody inputs still needed before live use. The P1 fixture has no operational authority.

Verification from `references/Hexalith.Platform`: the focused `dotnet build` command below exited 0 with zero warnings/errors; direct xUnit assembly execution passed 27/27 with zero skips. Platform `git -c core.whitespace=cr-at-eol diff --check` and root `git diff --check` exited 0. The new tests cover all three I/O matrix rows. Read-only hold inspection confirmed I3 still refuses with `authority-prerequisites-unapproved`, Story 27.4 remains `RECHECK_ONLY`/in-progress, A41 remains open, and Production telemetry deployments remain at zero replicas.

Three independent review layers reported eleven findings. Fixed the three focused test gaps by pinning literal body-size and media-type values and asserting null decoder outputs on refusal. The full Identity assembly passed 27/27 again with zero skips and both whitespace checks passed after the corrections. Two pre-existing protocol/adoption issues were recorded in `deferred-work.md`: independent receipt/status wire vectors and immutable-by-ID comparison across authenticated retrievals. The two earlier P1 deferred entries for evidence shape and body-size conflict are marked resolved for their offline scope. No commit, push or submodule pointer update was requested or made.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and evidence |
| --- | --- |
| Blind: evidence URI suggests provenance | false: `P1ReceiptVerifier` checks the locator against enrolled origin, and the result XML/documentation calls it the locator *supplied* for verification. No network provenance is claimed. |
| Blind: result omits authenticated verification time | false: this result binds exact bytes, not a standalone audit/authority record; the trusted time is still a separate required caller input and must be retained with the retrieval observation before operational use. |
| Blind: result omits root fingerprint | false: `P1AuthenticatedEnrollment.RootKeyFingerprint` remains available and the result binds its exact bootstrap payload digest; the result is not an independently adopted trust record. |
| Blind: bool API computes evidence digests | low, rejected: a successful offline bool call now hashes its bounded signed payloads again and may rehash the subject, but there is no operational hot path or production caller; splitting verification paths adds complexity for negligible current impact. |
| Blind: maximum frame test uses noncanonical payload | low, rejected: framing is tested independently at the exact body limit and canonical payload decoding is exercised separately; a near-1 MiB canonical receipt is uncommon and adds a large fixture without covering a distinct decoder branch. |
| Blind: no independent receipt/status wire vector | low, deferred: the canonical receipt/status wire encoder and decoder predate this change; a separate protocol-vector audit can cover shared encoding errors before deploying an issuer. |
| Blind: failure tests omit null output assertion | low, patch: wrong-type and malformed decoders already assign `document = null` before refusal, but focused tests should guard this public failure contract. |
| Blind: spec verification path is machine-specific | false: the path is a runnable command for this workspace, and the proposed fix only edits this build's spec rather than the implementation. |
| Edge: alternate valid signature under the same receipt ID | medium, deferred: the pre-existing pure verifier can accept a different valid issuer signature for the same claims; immutable-by-ID comparison requires an independently approved retained prior observation and live custody/issuer adapter. The new digest result enables that comparison but cannot establish first-use history. |
| Gap: limit test follows the production constant | low, patch: changing the constant could make the test pass while accepting a body over the approved 1,048,576-byte limit; assert fixed numeric boundaries. |
| Gap: media-type tests follow the production constants | low, patch: changing a constant could make tests pass while rejecting the documented v1 media type; use fixed literal positive and different-version negative inputs. |

## Design Notes

The signed payload verifier may accept 1,048,576 bytes offline. The HTTP response body cap is a separate, stricter approved limit: `4 + payload length + 64 <= 1,048,576`. The transport decoder therefore refuses an otherwise valid offline payload over 1,048,508 bytes. No live HTTP adapter can be considered operational before TLS/mTLS anchors, current root/key inventory, principal mapping, trusted time and immutable custody are independently enrolled.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Platform.Identity.Tests/Hexalith.Platform.Identity.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/memories/references/Hexalith.EventStore -p:NuGetAudit=false -m:1` from `references/Hexalith.Platform` — expected: zero errors.
- `dotnet tests/Hexalith.Platform.Identity.Tests/bin/Debug/net10.0/Hexalith.Platform.Identity.Tests.dll` from `references/Hexalith.Platform` — expected: all focused tests pass.
- `git -c core.whitespace=cr-at-eol diff --check` in Platform and `git diff --check` in Memories — expected: clean.
