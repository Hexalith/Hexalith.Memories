---
title: Verify the effective EventStore package floor
status: done
created: 2026-10-31
baseline_commit: 7352c6a2
---

# Effective EventStore package floor guards

Release 41191953588/job 124510372610 fails three existing AppHost security
configuration fixtures after the owner explicitly floors EventStore at 3.129.0.
The genuine selected Builds catalog still declares 3.128.4; the evaluated property
and restored Client item correctly select 3.129.0. Preserve both sources of truth:
validate the tracked shared import and its family indirection, the package-only
owner floor and family update, and the exact maximum of those tracked versions.
Keep rejection of foreign imports, arbitrary evaluated versions, Client-item
mismatches and project/mixed-version restored libraries. No production dependency
or source selection is changed by this fixture repair.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.Server.Tests/Deployment/AppHostSecurityConfigurationTests.cs` - verify effective shared/catalog floor and actual restore metadata
- `_bmad-output/implementation-artifacts/spec-effective-eventstore-package-floor.md` - record the bounded repair and proof

## Verification

Build and run the focused AppHostSecurityConfigurationTests in a disposable
workspace using the exact selected Builds commit 2186b9a4, effective EventStore
3.129.0 and unchanged shared catalog declaration. Existing wrong-version and
foreign-import controls must still fail closed; add a mismatched Client-item
control. Use the owning pinned commitlint and real Git hooks before publication.

## Cross-Tenant Negative Evidence

Not triggered: only package-policy fixtures and this spec change. Tenant routing,
authorization, data paths, catalog authority and runtime configuration are unchanged.

Actual focused verification uses the exact selected Builds 2186b9a4 catalog file:
shared declaration 3.128.4, effective owner property/Client item 3.129.0 and actual
restored EventStore family 3.129.0/type=package. Release build succeeds with zero
warnings/errors; all 15 AppHostSecurityConfigurationTests pass with no skips,
including wrong evaluated version, foreign import and mismatched Client-item
refusal controls. Logs: `/tmp/memories-3129-floor-build.log` and
`/tmp/memories-3129-floor-tests.log`. No original producer outputs were rebuilt.

During the mandatory pre-push fetch, upstream 7352c6a2 independently fixed the three
stale assertions with the same shared-pin/owner-floor semantics. Preserve that
commit unchanged; this follow-up adds only the explicit Client-item mismatch
negative control and this bounded evidence record. Recheck all 15 focused cases
against the upstream implementation before committing.

The upstream implementation plus the new negative control passes all 15 cases with
no skips; Release build remains zero-warning/zero-error. Final logs are
`/tmp/memories-3129-floor-upstream-build.log` and
`/tmp/memories-3129-floor-upstream-tests.log`.
