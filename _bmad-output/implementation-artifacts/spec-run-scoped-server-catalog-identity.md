---
title: Observe the actual run-scoped Memories application identity
status: done
created: 2026-10-31
baseline_commit: b23b597c27a70ea4cfafa2dc72dbdc4ed248014f
---

# Run-scoped server catalog identity

The existing private Development bootstrap passes literal memories as its
observation AppId even when the owning isolated topology selects a suffixed Dapr
identity. Read the existing EventStore:DomainService:AppId configuration contract,
falling back to the unchanged native/default memories identity. Keep SDK authority,
actual input capture, owner graph, finite scopes, authentication and all ACLs
unchanged. This does not create a grant, endpoint, observation or authority policy.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Program.cs` - capture the actual configured application identity through the existing SDK helper
- `_bmad-output/implementation-artifacts/spec-run-scoped-server-catalog-identity.md` - record the narrow correction and focused proof

## Verification

Build and run existing ServerEndpointAuthorizationTests against the real native
entry point in a disposable selected-Builds workspace. Those cases retain actual
unauthorized/authorized HTTP and secure actor/subscription boundary expectations.
Final catalog authority identity refusal and actual suffixed host proof belong to
the whole-root latest-cohort qualification, not a claim from this compilation.

## Cross-Tenant Negative Evidence

No tenant grant or access implementation changes. The existing auth-boundary
negative controls must remain green. A wrong or unbound identity continues to be
refused by the unchanged actual SDK observation/authority checks.

**Surfaces:** native Server entry point retains real HTTP tenant/authentication boundaries; only existing catalog observation identity binding changes.
**Tests:** `Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`.
**Command:** `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Release -m:1 -warnaserror`; `dotnet tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.Authentication.ServerEndpointAuthorizationTests`.
**Result:** all31cases pass with zero skips; Release compile has zero warnings/errors in `/tmp/memories-3129-floor-8wfmo2ol/Memories` using the actual selected shared catalog and owner floor. Existing missing/malformed tenant, unauthorized actor/subscription and allowed request cases remain enabled. Logs: `/tmp/memories-run-identity-build.log`, `/tmp/memories-run-identity-tests.log`. This is existing HTTP-boundary proof, not full catalog activation proof.
