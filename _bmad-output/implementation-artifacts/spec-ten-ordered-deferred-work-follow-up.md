---
title: 'Complete the next ten deferred work items'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Process open entries in ledger order, recording why any item is skipped. Deliver ten verified changes with one Conventional Commit per completed item. Run the solution build and all test projects before pushing the commits to `main`.

</frozen-after-approval>

## Implementation Notes

- Baseline `1af44955a0546652cef23eb59f54b58c3befa26f` has 269 open entries in the current numbered format and three numbered entries marked open in the legacy format. The previous sweep recorded skips through DW-239.
- Tenant configuration and ingestion items concern the existing HTTP conflict body, integration-fixture log generations, persisted data-member names, and the production dedup key builder.
- DW-249 stays open. The toolkit supports sidecar-specific callbacks. Removing the extra environment assignments would still leave Aspire inheriting the documented ambient credentials into child executables.
- DW-253 adds structured validation for CLI REST path identifiers. Query values, including source URIs, still use their existing path and query handling.
- DW-257 probes IPv4 and IPv6 loopback when available. DW-258 orders duplicate sample cases by newest creation time and ordinal ID. DW-259 checks the canary before the wizard starts its steps. DW-260 emits a safe probe diagnostic. DW-261 retains the Int32 wire type with bounded conversion.
- Focused verification passed 57 server tests, 75 CLI tests, and the provider lifecycle regression. The live tenant conflict, restart log isolation, and restart durability tests are in the integration suites.
- Ten numbered entries move to done. The ledger has 259 open entries in the current numbered format and three numbered entries marked open in the legacy format.

## Review Triage Log

- IPv6 address unavailable, patched. An IPv6-capable host without `::1` continues to pass the IPv4 check. An occupied IPv6 listener still fails the check.
- Actor JSON names, patched. The data contracts pin existing PascalCase DataMember names. The actor JSON attributes pin the existing camelCase keys, including the case counter's persisted state. Compatibility tests round-trip representative existing JSON.
- Case order before the limit, patched. The server and client agree on the ordinal tie-break. Reusing an older sample omitted by the pre-existing default list limit is deferred separately.
- Canary check timing, patched. The sample invariant is checked when the flow is constructed, before the wizard provisions a tenant. Its exact diagnostic is available in verbose output.
- Fixture registration evidence, patched. The nightly full-topology restart test now checks public cursors against a running replacement provider.
- Sidecar environment callbacks, evaluated. The current toolkit propagates the annotations but the installed Aspire runtime still inherits the original ambient token inputs. DW-249 records the outstanding compatibility dependency.
