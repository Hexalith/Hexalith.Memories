---
title: 'P1 receipt exact-byte verification hardening'
type: 'bugfix'
created: '2026-10-09'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/spec-pg2-c1-p1-authenticated-bootstrap-and-receipts.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** P1 receipt verification reads caller-owned signed bytes at different stages and compares expected claims through an encoder that rounds timestamps. That can make a positive offline verdict refer to bytes or times different from those checked. The receipt encoder also allocates oversized field bytes before enforcing its wire limit.

**Approach:** Verify receipt, status and subject from one local byte snapshot each; reject non-UTC or submillisecond receipt/status times instead of rounding; enforce the one MiB field limit before allocation. Add focused regression coverage while preserving the offline refusal boundary and all operational holds.

</frozen-after-approval>

## Implementation Notes

The Platform Identity verifier now snapshots bounded receipt/status payloads and signatures plus subject bytes before decoding, checking claims, hashing, and verifying signatures. `P1ReceiptWireV1` rejects non-UTC or submillisecond receipt/status times and checks the UTF-8 byte count before allocating a field buffer. The contract states the no-rounding rule; focused tests cover changed expected times and invalid encoder inputs. No operational enrollment, live call, or Memories I3 consumer was changed.

`dotnet build tests/Hexalith.Platform.Identity.Tests/Hexalith.Platform.Identity.Tests.csproj --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/memories/references/Hexalith.EventStore -p:NuGetAudit=false -m:1` from `references/Hexalith.Platform` passed with zero warnings/errors. Direct xUnit assembly execution passed 23 tests with zero failures/skips. `git -c core.whitespace=cr-at-eol diff --check` passed.

The review led to a multibyte oversized-field allocation check. The focused build and 23-test assembly run passed again. Two pre-existing API/transport questions were appended to `deferred-work.md`; they do not create an operational accepting path.

Platform implementation commit: `c489268fb347e7a65eedc6f2a4a40ab3ed4ce118` (Conventional Commit validated before and after).

## Review Triage Log

| Finding | Verdict and evidence |
| --- | --- |
| Subject snapshot may allocate a large buffer | low, rejected: a large subject is not invalid under the current contract, and the caller already holds its bytes. A cap would change accepted subject policy; the rare extra-memory case does not justify that change in this offline verifier. |
| A later caller may associate `true` with mutated original arrays | medium, deferred: the bool API cannot bind a later consumer to retained bytes; the contract already requires immutable retention. Record an evidence-shape decision for the future adapter. |
| Maximum HTTP body is smaller than maximum payload plus envelope | medium, deferred: the documented 1 MiB payload plus 68-byte envelope exceeds the documented 1 MiB body cap. This predates this change and needs a transport contract decision before an adapter exists. |
| No controlled concurrent-mutation test | low, rejected: the new snapshots are directly visible in the verifier, while a deterministic mid-call test would require a production test hook. A timing-based race test would be flaky and add little confidence. |
| Oversized-field test did not prove allocation behavior | medium, patched: the test now measures thread allocations for a 1.2 MiB UTF-8 field and requires less than 256 KiB during refusal; the previous allocate-then-reject code would fail. |
