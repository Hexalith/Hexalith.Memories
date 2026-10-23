---
title: Server algorithm registration review verification
status: done
baseline_commit: 750ffdacee973f4615c2741f196509618b9f80e4
---

Status: done

## Intent

Complete the registered-options verification requested by the approved Parties
five-item configuration/recovery task. Confirm programmatic algorithm selection
through the server composition root and distinguish configuration notifications
from the startup JWT bearer policy. Authentication policy changes require a
process restart. Keep the init-only public API and existing token controls.

This follow-up owns verification and clarification only. The prior source fix is
already committed in b65949b0ca5803b17e6f0761b146ff71e3529c27. The separate MCP
binding defect is deferred in the Parties task ledger. No release or deployment
is requested.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerRegisteredAlgorithmPolicyTests.cs` - registered programmatic and startup-policy tests authorized by the task.
- `tests/Hexalith.Memories.Server.Tests/Authentication/ProgrammaticServerAuthenticationOptionsFactory.cs` - seed the public init-only model before registered callbacks, without internal implementation access.
- `_bmad-output/implementation-artifacts/tests/five-owner-config-and-recovery-2026-10-23/README.md` - clarify the authentication restart requirement and test scope.
- `_bmad-output/implementation-artifacts/spec-server-algorithm-review-verification.md` - record this owner verification follow-up.

## Cross-Tenant Negative Evidence

**Not triggered:** This follow-up changes tests and operator clarification; it
changes no live claims, tenant routing or authentication implementation. Existing
signed-token acceptance and HS256 rejection controls remain in the focused suite.

### File List

- `tests/Hexalith.Memories.Server.Tests/Authentication/ServerRegisteredAlgorithmPolicyTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Authentication/ProgrammaticServerAuthenticationOptionsFactory.cs`
- `_bmad-output/implementation-artifacts/tests/five-owner-config-and-recovery-2026-10-23/README.md`
- `_bmad-output/implementation-artifacts/spec-server-algorithm-review-verification.md`

## Phase Ledger

| Date | Phase | Change | Test count | File List reconciliation |
|---|---|---|---|---|
| 2026-10-23 | create-story | Record the two confirmed verification gaps from independent Parties review and their approved test/doc follow-up. | Creation adds no executable cases; verification runs during development. | matched 3/3 planned paths; only the exact test, clarification and this record are owned. |
| 2026-10-23 | dev-story | Add the public-initializer OptionsFactory helper and registered programmatic selection; assert startup bearer policy persists across domain-option reload; clarify restart behavior. | Registered algorithm suite 8/8, zero failures/skips; one added programmatic case over the original 7-case suite. Command: dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*ServerRegisteredAlgorithmPolicyTests'; final output in /tmp/five-owner-review-memories-tests.log. | matched 4/4 changed paths; the helper is the fourth deliberately scoped path. |
| 2026-10-23 | code-review | Parent inspected the complete test/helper/clarification diff after the three-layer Parties review. B9 and B12 are corrected; the preexisting MCP ingress defect remains deferred in Parties. No production ABI or tenant behavior changes. | Final registered pipeline 8/8; public initializer and actual configure/postconfigure/validation callbacks run; named bearer options retain startup RS256 after domain options reload to ES256. | matched 4/4 final paths; test, helper, operator clarification and this record only. |

## Verification limits

The focused tests qualify this source registration. Earlier broad Server evidence
has two unrelated deployment/catalog failures, recorded in the Parties batch's
historical validation.json. Matching package publication/adoption and live remote
integration remain separate gates. No deployment, release dispatch or MCP change
is performed by this follow-up.
