---
title: 'Complete ten deferred work items in ledger order'
type: 'chore'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem.** The deferred-work ledger includes actionable findings, migration artifacts, and entries that depend on future events or decisions.

**Approach.** Process the ledger in order, implement and verify ten actionable items, and record the reason for each skip. Commit each completed item separately. Rebase onto current origin/main, run the full suite, and push on success.

</frozen-after-approval>

## Implementation Notes

- DW-31 adds a dated link from Epic 14 to the later Epic 15 plan and Story 15.5 reconciliation. Both links resolve. Review clarified the date of the original observation and linked directly to the completion notes.

- DW-35 documents the separator for multi-path ledger locations. The convention was checked against a migrated entry.
- DW-49 canonicalizes provider names for newly generated embeddings. A total of 104 activity and client tests passed.
- DW-107 exercises valid Google/Ollama reindex transitions in both directions. A total of 244 actor and provider registry tests passed.
- DW-132 bounds the process-local OIDC token cache to 1,024 keys. A total of 68 provider and cache tests passed.
- DW-133 caps token response buffering at 64 KiB and keeps malformed successful response content out of diagnostics. A total of 146 provider and client tests passed.
- DW-180 adds live MCP bearer and tenant isolation regressions. Seven live Aspire tests and six composition tests passed.
- DW-181 verifies the explicit stateless setting independently of the SDK default. Seven composition tests passed.
- DW-240 stops waiting for case names when the caller cancels. All 43 case service tests passed.
- DW-242 retains search results when Redis case-name enrichment times out or disconnects. A total of 76 service and endpoint tests passed.
- The ledger records ten completed items and the reason for each skip. It has 269 open entries at the end of implementation.

## Review Triage Log

- DW-35 distinguishes the legacy field name and preserves historical annotations.
- DW-49 uses provider-appropriate scalar fixtures and checks exact model spelling. Existing restore and migration formats retain their established behavior.
- DW-107 checks field membership independent of order and requires exactly one forced write.
- DW-132 documents the serialized prune-and-insert requirement and verifies an unrelated cached key survives eviction.
- DW-133 also covers invalid charset, an unpaired surrogate, and content without a declared byte length.
- DW-180 checks crossed tenant calls after matching calls and uses the production Redis key builder.
- DW-181 seeds a stateful value so deleting the explicit production override fails the test.
- DW-240 verifies that pre-cancelled calls never create a Redis batch.
- DW-242 verifies that a Redis command error still propagates. The HTTP test exercises hybrid search, and both search routes call the same case-name service.
