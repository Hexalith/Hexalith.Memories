---
title: 'PG-ONPREM-2 C1 P1 read-only receipt retrieval and status'
type: 'feature'
created: '2026-10-09'
status: 'in-review'
route: 'dispatch'
baseline_commit: '7b33e016a52907989181139298db18edef1a05d7'
review_loop_iteration: 6
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
- `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptHttpTrust.cs`, `P1ReceiptHttpHistory.cs`, `P1ReceiptHttpObservation.cs`, `P1ReceiptHttpResult.cs`, `P1ReceiptHttpFailure.cs`, `P1BootTimeClock.cs` — one-type-per-file transport scope, bounded history, suspend-aware clock and copied result types.
- `references/Hexalith.Platform/tests/Hexalith.Platform.Identity.Tests/P1LoopbackReceiptFixture.cs`, `P1ReceiptHttpAdapterTests.cs` — local TLS/mTLS and two-origin test surface.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Platform/src/Hexalith.Platform.Identity/P1ReceiptHttpAdapter.cs`, `P1ReceiptHttpTrust.cs`, `P1ReceiptHttpHistory.cs`, `P1ReceiptHttpObservation.cs`, `P1ReceiptHttpResult.cs`, `P1ReceiptHttpFailure.cs`, `P1BootTimeClock.cs` — implement strict GET chain, scoped TLS/mTLS, nonce, suspend-aware limits, bounded immutable-ID history and copied observation.
- [x] `references/Hexalith.Platform/tests/Hexalith.Platform.Identity.Tests/P1ReceiptHttpAdapterTests.cs`, `P1LoopbackReceiptFixture.cs` — prove every matrix row and the review cases below with local TLS/mTLS fixtures.
- [x] `references/Hexalith.Platform/docs/contracts/p1-receipt-provider-v1.md` — list independent Security/Operations inputs and fixture, clock and custody limits.
- [x] Memories readiness files — inspect holds without changing them.

**Acceptance Criteria:**
- Given local fixture facts and supplied scope/time, when both GETs complete, then exact copied bytes and evidence agree, the nonce is fresh, and no write request is sent.
- Given any transport, identity, authority, deadline or immutable-ID failure, when retrieval runs, then it returns no positive observation and does not follow another origin.
- Given only this prerequisite, when readiness is inspected, then I3 still refuses, Story 27.4 remains `RECHECK_ONLY`, A41 stays open and Production writes stay disabled.

## Implementation Notes

Transport trust must support either an exact CA/certificate anchor or an enrolled SHA-256 SPKI pin for a server chain element. Production always requires a valid system chain and exact DNS hostname in addition to that anchor; test-only loopback custom roots still require chain and hostname checks. Check every chain element's validity against caller-supplied authenticated UTC advanced by the monotonic elapsed time, including at each TLS handshake. Verify the client chain at each GET with that time, require an explicit client-auth EKU in the leaf, the exact sole SPIFFE URI SAN, and a completed TLS stream that mutually authenticated the exact enrolled client certificate before HTTP. Never allow fixture trust through a public production constructor.

Use Linux `CLOCK_BOOTTIME` for request and 300-second chain elapsed time and authenticated-UTC advancement; it includes host suspension. Fail closed when a suspend-aware same-boot clock is unavailable or fails, with no `Stopwatch`/system-wall-clock fallback. An internal clock seam may simulate elapsed jumps in tests. Check the clock after every awaited transport/body operation and immediately before success/history commit; combine a cancellation token with the boottime checks for prompt I/O cancellation. If caller cancellation is signalled at final commit, return no observation and do not seed history.

Neither TLS nor chain validation may fetch CRLs, OCSP, AIA issuers or any other address outside the enrolled GET origins. Use only offline revocation verification from an independently provisioned current CRL cache for production server and client chains, and refuse when evidence is missing or a platform implements offline mode with network access. Local generated fixture roots may use no-check solely in the internal loopback path. Do not use online revocation mode or treat a two-second URL timeout as a network boundary. Require an explicit server-auth EKU in each server leaf as well as the server-auth chain policy. Certificate validation must finish or refuse within the same per-request and whole-chain bounds; no positive result may follow a timed-out validation.

Accept a response only when it has one exact type, `no-store`, no `public`, `max-age`, `s-maxage`, `stale-while-revalidate`, `Expires`, `Age`, `Cache-Status`, `X-Cache`, `CDN-Cache-Control`, `Surrogate-Control`, encoding, partial body or redirect. Retain the first verified framed receipt for every ID the instance accepts, with a fixed bounded memory budget and no eviction; refuse new IDs when full. Compare full framing, including the signature, with both a supplied prior observation and instance history before status. Check the final 300-second boottime deadline before recording success; a refused request must not seed history. Caller-supplied history across instances is necessary because process memory is not custody.

Fixture verification must cover production-constructor refusal of a self-signed local root, a second status origin with a distinct server anchor and client identity plus wrong-status-trust refusal, a server that never requests a client certificate, absent client-auth EKU, certificate expiry against authenticated UTC and between GETs, and contradictory/cache-indicator headers. Assert mTLS and `no-store` separately for both GETs, exact returned status framing and evidence hashes, and an immutable-ID incident from a changed signature over identical receipt payload. Test bounded history exhaustion and ensure a refused retrieval does not seed it.

Also test wrong server-auth EKU and DNS SAN, a public production trust instance against a loopback TLS leaf, server-only expiry and client-only expiry separately, status-byte defensive copying after mutation, cancellation at final acceptance, suspend/300-second elapsed jumps through the internal clock seam, and no history insertion after those refusals. Test an offline CRL/AIA distribution URL that would be observable if fetched; verification must refuse without making that extra request. Keep supplied server intermediates in the authenticated-time chain rebuild and test an SPKI mismatch under the same trusted root.

Reject a null client issuer during trust construction, even when the C# parameter is nonnullable, and require a built client chain to contain the exact enrolled issuer. Validate SPIFFE IDs as canonical `spiffe://` trust-domain paths without user info, query or fragment; the client leaf must contain exactly one URI SAN with that ID. An SPKI pin must hash the certificate's actual SubjectPublicKeyInfo bytes for every supported key algorithm; if an algorithm cannot be exported, refuse it before pin comparison and never hash an empty substitute. Refuse on 32-bit Linux unless native `timespec` layout is explicitly verified, and convert missing libc/clock entry points into ordinary deadline refusal.

A boottime deadline must cancel a pending connect, TLS, response-header or body await promptly after host resume; wall-clock `CancelAfter` alone is insufficient. Use a Linux `CLOCK_BOOTTIME` based deadline signal or equivalent monitor and retain explicit boottime checks after awaits. Construct the copied candidate observation before the final clock/cancellation and history commit, then perform no expensive allocation between that final check and returning it. A rejected or expired attempt must leave history unchanged. Document the narrow unavoidable gap between a final check and the caller receiving the result; do not claim an atomic real-world return instant.

Production trust must enforce offline revocation and current CRL validity from the platform cache, while Security/Operations separately attest cache provenance and refresh. The adapter does not authenticate that operational attestation and must not describe ambient cache state as independently proven. Isolate tests so missing CRL evidence alone refuses an otherwise valid client chain and production server system-root validation is reached during a real local TLS handshake through a controlled internal fixture seam; keep the public constructor unable to adopt a loopback root. Test same-instance changed valid signature with no supplied prior observation. Treat any response header whose name contains `cache` (case-insensitive), except the single exact `Cache-Control: no-store`, as a cache indicator and refuse it; include `CF-Cache-Status` and `X-Cache-Status` fixtures.

Synchronous `X509Chain.Build` must not hold the public retrieval beyond the 10-second GET or 300-second chain deadline. Run validation in a bounded worker that the boottime deadline can stop waiting for; a validation that continues after refusal must hold a per-trust concurrency slot until it truly ends, so repeated requests cannot accumulate abandoned workers. Refuse if a validation slot is occupied. Use a fresh authenticated UTC from the same-boot clock both before and after each build, and refuse if any chain element or available revocation evidence has expired at the post-build instant. Recheck the boottime request and chain limits before and after each validation. A TLS callback must fail promptly when its bounded validation expires, with no HTTP bytes emitted.

Allow explicitly supplied client intermediates and rebuild the client chain through them to the exact enrolled issuer on every GET. Supply the same chain in the TLS credential context so mTLS can present it; do not rely on ambient intermediate discovery or AIA downloads. Check deadline/cancellation immediately before returning an immutable-ID incident. Immediately before a successful history commit, use the newly advanced authenticated UTC to recheck receipt, enrollment and status expiry/freshness (including the status's exclusive expiry), without rerunning unbounded transport work. A rejected final check must not seed history.

Cover a public production trust's server-chain guard directly as well as the controlled real-handshake seam: missing client CRL must not be the reason that server validation appears tested. Add a local isolated positive production-trust path with an ephemeral system-trusted root and current offline CRL evidence if the platform permits it; never mutate a host-wide trust store. If the test environment cannot provision that path, record the exact limitation and retain explicit production refusal. Include a validly signed wrong-scope receipt that refuses before status and without an incident, a history-full ID retried with new valid framing, the receipt-signature evidence digest, and a null receipt-ID refusal. Make loopback gate callbacks cancellable on fixture disposal so assertion failures cannot hang test cleanup.

Revalidate the exact client issuer chain and exclusive certificate expiry against freshly advanced authenticated UTC immediately after each completed TLS handshake and before emitting any HTTP bytes; validation before context creation alone is insufficient. The second validation must use the same bounded worker and deadline. Verify an expiry jump during context creation or handshake refuses with zero HTTP requests. Require exact DNS hostnames for production and fixture trust; IP-literal origins are invalid. Supply an internal fixture root independently from the enrolled server SPKI pin so pin-only trust has a positive local test without weakening the public system-root requirement. Refuse any client chain element between leaf and enrolled issuer that is absent from the caller-supplied intermediate set, even if a local machine store contains it. Add a server-intermediate fixture to prove the TLS-supplied chain is retained by authenticated-time rebuilding.

Bound history by both retained framed bytes and an explicit maximum ID count that covers dictionary, ID-string and array overhead; document the frame-byte and entry limits accurately instead of calling frame bytes the total process memory. Keep the first accepted ID without eviction, and refuse new IDs at either limit. Test final receipt expiry separately from status expiry, separately expired server and client certificates at initial authenticated UTC, no observation on every final refusal, a server-chain worker stalled inside a real TLS callback, and pin-only success plus same-root wrong-pin refusal. Keep final cancellation, history-full retry, signed wrong-scope refusal, AIA/CRL trap, and pending boottime-jump cases. A positive public production trust test remains conditional on isolated system-root/current-CRL provisioning; document the missing evidence without suggesting local fixtures establish it.

## Spec Change Log

- 2026-10-09 review loop 1: The trust, cache, time, history and coverage findings in the Review Triage Log exposed underspecified implementation details outside the approved intent. Expanded Code Map, tasks and Implementation Notes to require explicit EKU, both anchor forms, authenticated-time chain checks, per-GET mTLS, contradictory cache refusal, bounded fail-closed history and two-origin/evidence tests. Avoid the known-bad state of accepting a partly scoped transport response or reporting an incident from a refused retrieval. KEEP: pre-HTTP mutual-authentication confirmation, no redirect/proxy/cookie/decompression defaults, exact framed bytes and defensive copies, fresh status challenge, 10-second GETs and 300-second monotonic chain, offline verifier composition, fail-closed observations, local loopback fixtures and explicit no-custody/no-authority documentation.
- 2026-10-09 review loop 2: Suspend-aware timing, uncontrolled online revocation egress, missing server-auth EKU and cache metadata exposed gaps in the prior Implementation Notes. Added `P1BootTimeClock`, offline-only revocation, explicit server purpose, extra cache-header refusal and focused independent clock/identity tests. Avoid the known-bad state of a suspended host accepting stale evidence or certificate validation contacting an unapproved CRL/OCSP origin. KEEP: both server anchor forms, exact two-origin trust, supplied intermediates, explicit client EKU/SPIFFE, pre-HTTP mTLS, bounded non-evicting history, full-framed immutable-ID comparison, defensive receipt/status/subject copies, fresh challenge, canonical offline verifier and the passing local fixture cases. Patches and tests from lower-priority review entries are carried into the new fixture instructions rather than applied to reverted code.
- 2026-10-09 review loop 3: Null client issuer fallback, pending-I/O suspend gaps, empty SPKI fallback, invalid SPIFFE grammar, ambient CRL provenance claims, incomplete production/history tests and CDN cache indicators exposed gaps in trust and deadline instructions. Expanded Implementation Notes to require mandatory issuer binding, canonical SPIFFE IDs, actual SPKI or refusal, a boottime-driven pending-I/O deadline, final copied-result preparation before commit, explicit external CRL attestation limits, isolated production/instance tests and conservative cache indicators. Avoid the known-bad state of accepting an unenrolled system-trusted client, hashing empty SPKI, or leaving suspended I/O indefinitely pending. KEEP: the 64 passing local Identity tests, two distinct origins and mTLS identities, supplied intermediates, explicit EKUs, offline chains, exact server anchor or SPKI, Linux boottime checks, bounded non-evicting history, full-framed immutable-ID comparison, defensive bytes, fresh nonce, canonical offline verifier, strict response framing and unchanged readiness holds. Lower-priority review patches are carried into the new fixture instructions.
- 2026-10-09 review loop 4: Synchronous chain validation, pre-build certificate time, final status expiry and absent client intermediates exposed gaps in the timing and trust instructions. Expanded Implementation Notes for bounded, concurrency-limited validation workers, post-build authenticated-time checks, explicit client-intermediate presentation, final status freshness and isolated production/negative-path tests. Avoid the known-bad state of a stalled chain build outrunning the GET limit or an expired status passing at final acceptance. KEEP: the 81 passing Identity tests, two-origin local TLS/mTLS, pre-HTTP client/server authentication, offline CRL/AIA no-egress, actual RSA/ECDSA SPKI, mandatory issuer/SPIFFE/EKUs, boottime pending-I/O monitor, exact no-store framing, bounded history, defensive evidence copies, fresh nonce, canonical verifier and unchanged readiness holds. Lower-priority review patches are carried into the new fixture instructions.
- 2026-10-09 review loop 5: Client expiry between preflight and TLS, frame-only history budgeting, pin-only fixture refusal and ambient client-intermediate acceptance exposed remaining trust and bounded-memory gaps. Expanded Implementation Notes for a second bounded client validation immediately before HTTP, DNS-only origins, separate fixture root for pin-only tests, exact supplied-intermediate membership, ID-count and byte limits, and isolated initial/final expiry and stalled-callback cases. Avoid the known-bad state of an expired mTLS client reaching HTTP, a pin-only promise that cannot succeed in local fixtures, or history metadata growing beyond its stated budget. KEEP: the 59 passing Identity tests and subsequent no-egress/context-bound fixes, two distinct origins and mTLS identities, offline CRL/AIA trap, actual SPKI and supplied client intermediates, explicit EKUs/SPIFFE, boottime and bounded validation, no-store exact frames, fresh status challenge, immutable-ID incidents and no-seeding refusals, defensive bytes, unchanged readiness holds and documented production trust limitation. Lower-priority review patches are carried into the new fixture instructions.

## Review Triage Log

Review loop 1 (blind hunter, edge-case hunter, verification-gap reviewer). Each row records one finding before grouping; duplicate claims retain their own rows.

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1 missing explicit client-auth EKU | medium — `ValidClientIdentity` relies on `ApplicationPolicy`; a leaf without an EKU extension is unrestricted. | bad_spec |
| BH2 public-key anchor option | medium — server validation compares full certificate `RawData`, so a renewed certificate with the same enrolled SPKI cannot satisfy the documented key-anchor option. | bad_spec |
| BH3 contradictory cache control | medium — `NoStore` alone permits `public`/`max-age` and `Expires` metadata on the same response. | bad_spec |
| BH4 history before deadline | medium — `TryAdd` runs before the final deadline check, allowing a refused retrieval to seed ID history. | bad_spec |
| BH5 unbounded ID history | high — each new ID retains up to 1 MiB forever in the instance dictionary; a long-lived caller can exhaust memory. | bad_spec |
| BH6 client expiry between GETs | medium — the credential chain is checked only at the supplied start time, not the later status handshake time. | bad_spec |
| BH7 two-origin trust coverage | medium — every HTTP fixture enrolls the same origin and uses the single-trust constructor, leaving the separate status identity path unproven. | bad_spec |
| BH8 aggregate request assertions | low — fixture `|=` flags allow one compliant GET to mask a noncompliant second GET. | bad_spec |
| BH9 additional status refusals | low — tests omit status truncation, oversize, timeout, duplicate type and signature tampering, but the same bounded HTTP reader is tested on receipts and status decoding/verifier refusal are separately exercised; duplicating every case adds fixture complexity with little new coverage. | reject |
| BH10 signature-only ID change | low — the test changes payload bytes, so it does not prove that a changed detached signature alone is an incident despite the current full-byte comparison. | bad_spec |
| VG1 production trust test | medium — all current fixture trust uses internal loopback mode; removing the production system-chain guard would leave its tests green. | bad_spec |
| VG2 separate status origin test | medium — no test calls the two-trust constructor with distinct origins; using receipt trust for status would remain undetected. | bad_spec |
| VG3 retained status-byte evidence | medium — the success test never compares returned status framing or its payload/signature hashes with the served body and evidence. | bad_spec |
| VG4 cache-indicator tests | medium — `Age`, `Cache-Status` and `X-Cache` guards are untested while `no-store` is present. | bad_spec |
| EH1 missing explicit client-auth EKU | medium — the leaf EKU extension is not inspected; the chain policy alone can accept a purpose-unrestricted leaf. | bad_spec |
| EH2 contradictory cache metadata | medium — `no-store` with `public`, `max-age`, or `Expires` reaches the accepted path. | bad_spec |
| EH3 credential expiry or revocation between GETs | medium — `ValidClientIdentity` is not repeated at status request time; its later validity is unknown even if the server accepts the handshake. | bad_spec |
| EH4 deadline after ID insertion | medium — `TryAdd` precedes the final 300-second check and can affect a subsequent retrieval after a refused one. | bad_spec |
| EH5 server expiry under clock skew | high — production TLS validation uses system chain time only; an independently authenticated later UTC can be past certificate expiry while system time still accepts it. | bad_spec |

Review loop 2 (all findings individually triaged before grouping):

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1 suspend-aware clock | high — .NET 10 Linux `Stopwatch` uses `CLOCK_MONOTONIC`, which excludes suspension; the adapter can resume with less than actual elapsed time and stale authenticated UTC. | bad_spec |
| BH2 revocation network egress | high — production `X509RevocationMode.Online` may download CRLs beyond the two enrolled origins; `DisableCertificateDownloads` controls AIA issuer downloads only. | bad_spec |
| BH3 synchronous revocation deadline | medium — `X509Chain.Build` is synchronous and ignores the request token; online retrieval can delay refusal beyond the 10-second request deadline. | bad_spec |
| BH4 unsigned friend assembly | low — the internal fixture factory is callable by a same-name unsigned assembly, but code already running inside the trusted consumer process can supply arbitrary enrollment/trust or use reflection; this is not a meaningful external boundary here and a signing redesign adds complexity. | reject |
| BH5 other cache metadata | medium — `stale-while-revalidate`, `CDN-Cache-Control` and `Surrogate-Control` are not rejected despite the no-cache response rule. | bad_spec |
| BH6 origin grammar mismatch | false — `P1BootstrapVerifier.ValidOrigin` and `P1ReceiptVerifier.ValidOrigin` already require the same exact `GetLeftPart(UriPartial.Authority) == origin` canonical form. | reject |
| BH7 cancellation at commit | medium — cancellation can arrive during synchronous final verification; `Commit` only checks chain time, so a cancelled call can still return and seed success. | patch |
| BH8 production trust test | medium — the direct validator test supplies a root as the peer certificate and does not run the public constructor through a real TLS handshake. | patch |
| BH9 independent expiry test | medium — fixture `notAfter` expires both server and client certificates, so neither recheck is isolated. | patch |
| BH10 300-second deadline test | medium — no test drives the adapter chain deadline; a fake suspend-aware clock is needed to verify refusal and no history. | bad_spec |
| BH11 duplicate status transport cases | low — the shared bounded `GetAsync` reader already runs on both paths, receipt-side failures exercise it and offline status decoder tests run; duplicating every refusal fixture adds complexity with little new assurance. | reject |
| BH12 status copy assertion | low — the success test mutates a returned status array but does not read it again; a one-line comparison would catch a shared-array getter. | patch |
| EH1 server-auth EKU | high — authenticated-time `X509Chain.Build` omits server-auth application policy and explicit leaf EKU, so a wrong-purpose certificate can pass the custom callback. | bad_spec |
| EH2 suspend-aware clock | high — `Stopwatch` excludes Linux suspend time and undercounts both chain deadline and authenticated-time advancement. | bad_spec |
| EH3 isolated client expiry | medium — the claimed client-expiry test also expires the server leaf, allowing server refusal to mask a missing client recheck. | patch |
| VG1 hostname regression test | medium — all loopback server leaves use `localhost`; removing the name-mismatch check would leave tests green. | patch |
| VG2 independent expiry tests | medium — shared expiry masks a regression in either the server or client authenticated-time validation. | patch |
| VG3 status defensive copy test | low — mutation is followed by receipt and subject comparisons only, so returning `_status` directly would go undetected. | patch |

Review loop 3 (all findings individually triaged before grouping):

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1 pending I/O after suspension | medium — `CancelAfter` is not checked against `CLOCK_BOOTTIME` while `SendAsync` or body reads are pending; the next explicit clock check runs only after the await completes. | bad_spec |
| BH2 final-copy deadline gap | medium — `Commit` checks time before constructing and copying the returned observation; suspension during copying can produce a positive result after the deadline. | bad_spec |
| BH3 certificate expiry during response body | false — TLS certificates are authenticated for each handshake, as the current Implementation Notes require; the protocol does not require continuous certificate validity after a completed handshake. | reject |
| BH4 CRL cache provenance | medium — `Offline` consumes an ambient cache with no API evidence that Security/Operations independently provisioned and refreshed it; operational provenance remains caller-owned but the public trust surface cannot enforce the stated prerequisite. | bad_spec |
| BH5 production system-root test | medium — production `ValidClient` can refuse before the loopback server handshake, so the current public-constructor test does not exercise the system-root server guard. | bad_spec |
| BH6 unsupported-key empty SPKI | high — `ExportSpki` returns an empty array for non-RSA/non-ECDSA keys, making the SHA-256 of empty bytes a matching pin for any otherwise valid unsupported-key chain element. | bad_spec |
| BH7 additional SAN identities | false — the requirement is one exact SPIFFE URI SAN, not that the certificate has no DNS or IP SAN; the code counts URI SAN entries and compares the sole URI exactly. | reject |
| BH8 SPIFFE URI grammar | medium — trust construction accepts user info, query or fragment while checking only scheme and host; those forms are invalid SPIFFE IDs even if a matching SAN is supplied. | bad_spec |
| BH9 unlisted cache indicators | medium — `ValidHeaders` rejects a fixed list but accepts `CF-Cache-Status: HIT` and `X-Cache-Status: HIT` with `no-store`. | bad_spec |
| BH10 32-bit Linux timespec | maybe-false — the interop assumes two 64-bit native fields; a supported 32-bit runtime/ABI would need verification to establish whether its `timespec` matches that layout. | defer |
| BH11 cancelled validation tasks | maybe-false — `WaitAsync` does not stop a running `X509Chain.Build`; a reproducibly stalled offline build would establish whether abandoned work can accumulate materially. | defer |
| BH12 unawaited fixture handlers | low — `ServeAsync` discards connection tasks, so a handler may overlap fixture disposal; this has no observed ordinary-test failure and tracking/draining tasks adds lifecycle state. | reject |
| BH13 prior review origin statement | low — `P1ReceiptVerifier.ValidOrigin` lacks `GetLeftPart` equality, but bootstrap verification already enforces canonical origins before creating `P1AuthenticatedEnrollment`; correcting this review log alone is a spec edit, which this review must reject. | reject |
| VG1 instance-only history coverage | medium — changed-signature tests always pass `priorObservation`; removing `History.Compare` byte comparison would leave those tests green for callers relying only on instance history. | patch |
| VG2 isolated production CRL coverage | medium — both production refusal tests can fail from the untrusted server, so changing production client revocation to `NoCheck` could leave them green. | patch |
| EH1 null client issuer | high — the constructor permits null at runtime, `NewChain` then omits `CustomRootTrust`, and `ValidChain` skips the enrolled-root comparison; a system-trusted client chain can pass. | bad_spec |
| EH2 pending boottime cancellation | medium — the same pending-await gap as BH1 occurs because `CancelAfter` is not driven by `CLOCK_BOOTTIME`. | bad_spec |
| EH3 missing libc/entry point refusal | medium — `P1BootTimeClock.Now` can throw `DllNotFoundException` or `EntryPointNotFoundException`, neither caught by `RetrieveAsync`, so an unavailable clock faults instead of returning a refusal. | patch |
| EH4 unsupported SPKI algorithm | medium — an authentic SPKI pin for a valid non-RSA/non-ECDSA chain element is compared to the hash of empty bytes and refused. | bad_spec |
| EH5 null issuer claim | high — same null-issuer path as EH1; no constructor guard or mandatory enrolled-root check blocks it. | bad_spec |
| EH6 pending request claim | medium — same pending-await gap as BH1 and EH2; the explicit boottime check follows completion of the await. | bad_spec |

Review loop 4 (all findings individually triaged before grouping):

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1 synchronous chain deadline | high — `ValidateClient` and the TLS callback run `X509Chain.Build` synchronously; cancelling the transport token cannot interrupt a build that stalls past the 10-second request or 300-second chain limit. | bad_spec |
| BH2 time captured before chain build | medium — `ChainValid` sets verification time and checks element validity using the pre-build `now`; a certificate or CRL expiring during a slow build can pass before the request limit. | bad_spec |
| BH3 status expiry before final acceptance | medium — `TryVerify` uses an earlier authenticated time, while final `Commit` checks only the chain deadline; a status can expire during later copying or validation work. | bad_spec |
| BH4 incident without final cancellation check | medium — after receipt verification, a differing prior/history frame returns an incident without checking cancellation or boottime again, so a cancelled attempt can misclassify the retrieval. | patch |
| BH5 missing client intermediates | medium — `ValidateClient` supplies the leaf and enrolled issuer only; a leaf signed through an intermediate below that issuer depends on ambient stores and the TLS credential supplies no explicit intermediate chain. | bad_spec |
| BH6 ETag/Last-Modified headers | false — those validators do not establish that this response was cached; the required `no-store` and cache-indicator checks remain in force. | reject |
| BH7 public production system-root test | medium — public trust refuses at missing client CRL before server TLS, while the server-root test uses an internal trust seam; the public server-chain guard is not independently regression-tested. | patch |
| BH8 production positive-path test | medium — all public production cases refuse; a regression that makes production trust always refuse would pass, although no operational CRL evidence or root is available in this workspace. | bad_spec |
| BH9 status cache-header duplication | low — the same `ReadResponseAsync` parser handles both origins; receipt-side cache refusals and status signature/nonce cases run, so duplicating every status header case adds fixture complexity with little value. | reject |
| BH10 receipt-signature evidence assertion | low — the success test checks other evidence hashes but omits the receipt signature digest; a direct assertion is a one-line correction. | patch |
| BH11 Code Map inventory | low — the nonfrozen Code Map omits two newly added helper files, but this finding's only fix edits the build spec. | reject |
| VG1 public server-chain guard coverage | medium — preverified gap: public constructor tests stop at client CRL failure, and the real-handshake guard test uses an internal constructor. | patch |
| VG2 signed wrong-scope pre-status test | medium — preverified gap: offline wrong-scope tests do not call `VerifyReceiptOnly`; no adapter test serves a validly signed wrong-scope receipt before status. | patch |
| VG3 rejected history-full ID retry | medium — preverified gap: the budget test retries the previously accepted ID, never the refused new ID, so a rejected ID could be retained unnoticed. | patch |
| EH1 null receipt ID | medium — `Hex(expected.ReceiptId)` runs before the `try` and dereferences a nullable runtime value; a malformed public record throws instead of returning a refusal. | patch |
| EH2 synchronous chain deadline claim | high — same synchronous `X509Chain.Build` path as BH1 cannot be interrupted by the boottime monitor token. | bad_spec |
| EH3 gated fixture disposal | medium — fixture callbacks await uncancellable gate tasks, while disposal waits for all request tasks; a failing test before gate release can hang its cleanup. | patch |

Review loop 5 (all findings individually triaged before grouping):

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1 client expiry during handshake | high — client chain validation occurs before context creation and TLS; an authenticated-time jump during those operations can leave an expired client credential serving HTTP without a second check. | bad_spec |
| BH2 repeated production chain builds | low — the same-second loop can refuse a valid but consistently slow chain before the 10-second limit, yet this is fail-closed and a more permissive CRL freshness proof would add substantial complexity for an unusual case. | reject |
| BH3 history metadata outside budget | medium — `P1ReceiptHttpHistory` charges only frame bytes and has no ID-count cap, so ID strings and dictionary/array overhead can materially exceed the documented 16 MiB memory budget. | bad_spec |
| BH4 platform offline no-egress gate | false — the adapter requires Linux boottime, `X509RevocationMode.Offline` only uses cached revocation data, and both context creation and TLS chain policy disable missing-certificate downloads. | reject |
| BH5 IP-literal origin | medium — trust construction accepts an IP-literal HTTPS origin/hostname even though the required server identity is an exact DNS hostname. | patch |
| BH6 pin-only success | medium — the fixture custom-root path dereferences `ServerAnchor` for chain trust, so a pin-only fixture cannot validate even with an independently supplied trusted local root; the alternate public pin path lacks positive coverage. | bad_spec |
| BH7 production positive test | false — the frozen intent limits testing to local fixtures and forbids live operational inputs; Implementation Notes explicitly permit recording the unavailable isolated system-root/current-CRL fixture, which the contract does. | reject |
| BH8 initial certificate expiry tests | medium — expiry between GETs is tested, but separate server/client expiry at the initial authenticated UTC is not; either first-handshake check could regress undetected. | patch |
| BH9 stalled server callback validation | medium — the worker is tested directly, but no TLS callback test stalls server-chain validation and proves prompt refusal with no HTTP bytes. | bad_spec |
| BH10 server intermediate fixture | medium — the code consumes TLS-supplied intermediates but no loopback case builds a server chain through one, so dropping that path could pass all current tests. | bad_spec |
| VG1 final receipt expiry coverage | medium — preverified gap: final status expiry is tested, while removing the final receipt-expiry condition would leave tests green. | patch |
| VG2 null observation after final refusal | medium — preverified gap: final expiry, cancellation and capacity tests inspect failure but not the public observation field, so a leaked candidate could pass. | patch |
| VG3 production positive retrieval | false — the required isolated system root and current offline CRL are unavailable here, and the spec explicitly allows this documented limitation under local-only testing. | reject |
| EH1 client expiry before HTTP | high — same validation-to-handshake gap as BH1 permits an expired client certificate under authenticated UTC. | bad_spec |
| EH2 ambient client intermediate | medium — CustomRootTrust still permits finding a missing intermediate in local certificate stores; the built chain is not checked to ensure every intermediate came from the supplied set. | bad_spec |

Review loop 6 attempted (fifth and final review pass; all findings individually triaged before grouping):

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1 malformed header line break | medium — `ReadResponseAsync` permits CR/LF inside an unknown header value, then splits only on CRLF; a bare LF can conceal a cache-looking field from the name checks and accepts malformed HTTP. Rejecting an unpaired line break is a direct parser correction. | patch |
| BH2 TLS resumption | medium — `AllowTlsResume` defaults to true and this code leaves it enabled; a resumed Linux .NET 10 mTLS connection can report no local certificate and refuse a valid second GET. The post-handshake guards still prevent an unauthenticated positive result. Disabling resumption is a direct option correction. | patch |
| BH3 existing-ID final guard | medium — `Commit` checks time before comparing an existing frame but returns immediately after comparing up to 1 MiB; expiry or cancellation during that comparison can return success. A final guard after the comparison is a direct correction. | patch |
| BH4 handshake-time client expiry test | medium — the context-created clock jump tests the second client check, but no clock jump occurs inside TLS; an absent post-handshake check could escape the existing test. The existing server-chain callback hook can induce the jump. | patch |
| BH5 status server without client-certificate request | false — both GETs use the same `GetAsync` guard requiring `tls.IsMutuallyAuthenticated` and the exact enrolled local certificate; removing that guard would fail the existing receipt-server negative test, and the status success test also asserts mutual authentication. There is no status-only branch that bypasses it. | reject |
| BH6 server CRL/AIA network trap | medium — the sole trap URL is on the client leaf and only `ValidateClient` is exercised; server-chain policy or TLS callback egress could regress undetected. A real callback test needs a server URL certificate variant and controlled production-trust path. | bad_spec |
| BH7 pending TLS and body deadlines | medium — `PendingBodyRefusesAfterBoottimeJump` actually waits in `BeforeResponse` before headers, so it covers a header wait; no test holds TLS or a partial body after headers. A partial-body fixture seam and handshake gate are needed to prove those waits. | bad_spec |
| BH8 strict HTTP length and transfer framing tests | medium — parser code rejects duplicate length, transfer encoding, truncation and extra bytes, but current fixtures emit matching length and body and do not exercise those refusals; body-shape variants need new fixture behavior. | bad_spec |
| BH9 status refusal history retry | medium — status refusal tests assert no observation but never retry a changed valid receipt on the same adapter, so premature history insertion on that path would be missed. Existing fixture mutation supports a direct retry test. | patch |
| BH10 stalled client-context worker | medium — `ContextCreated` advances time but returns immediately; no test blocks it and checks prompt refusal plus occupied validation slot. The existing hook and gate pattern support a direct test. | patch |
| VG1 mismatched client SPIFFE SAN | medium — constructor tests reject malformed configured strings, while every certificate SAN matches its configured ID; removing `ValidClientSan` could leave those tests green and allow an unenrolled URI SAN. A valid mismatched configured ID can be tested with the current fixture. | patch |
| VG2 partial-body pending read | medium — preverified gap: `BeforeResponse` runs before headers and body, so the named pending-body test cannot detect an uncancelled body `ReadAsync`; the fixture needs an after-header partial-body stall. | bad_spec |
| VG3 stalled context creation | medium — preverified gap: the context hook never blocks, so synchronous or unbounded context creation could outrun the GET limit unnoticed; an existing hook can be gated in a direct test. | patch |
| VG4 server-side CRL/AIA egress | medium — preverified gap: the trap URL appears only on the client leaf; server callback egress has no observable trap fixture and needs a server certificate URL variant plus real callback test. | bad_spec |
| EH1 initial clock-failure classification | medium — the initial `_clock.TryRead` is folded into invalid-input validation and returns `InvalidInput`; Implementation Notes require missing clock entry points to yield an ordinary `Deadline` refusal. The returned observation remains null, but callers receive the wrong failure reason. | patch |

The surviving `bad_spec` entries are: server CRL/AIA egress coverage (BH6, VG4), pending TLS and partial-body deadline coverage (BH7, VG2), and strict HTTP framing coverage (BH8). They require new fixture behavior and revised verification instructions; the remaining `patch` entries are held by the cascading loopback rule. Incrementing the review iteration from 5 to 6 exceeds the bmad-build limit of five, so work halts for human escalation without reverting the current implementation.

## Design Notes

An instance remembers observed IDs; a caller can pass a prior retained observation across instances. Durable history needs authenticated external custody. Production TLS requires hostname/system chain validation plus enrolled anchor; test-only loopback roots still require chain and hostname checks.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Platform.Identity.Tests/Hexalith.Platform.Identity.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/memories/references/Hexalith.EventStore -p:NuGetAudit=false -m:1` from `references/Hexalith.Platform` — expected: zero errors.
- `dotnet tests/Hexalith.Platform.Identity.Tests/bin/Debug/net10.0/Hexalith.Platform.Identity.Tests.dll` from `references/Hexalith.Platform` — expected: all Identity tests pass.
