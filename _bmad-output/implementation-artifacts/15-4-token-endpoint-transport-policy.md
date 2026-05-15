# Story 15.4: Token Endpoint Transport Policy

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a security-conscious operator,
I want OIDC token endpoint transport rules to distinguish local development from production,
so that production token acquisition cannot silently use insecure transport.

## Acceptance Criteria

1. Given local Keycloak and fake-server tests may use `http://localhost`, when token endpoint validation sees loopback HTTP endpoints, then the local/development path remains explicitly supported and covered by tests.

2. Given production token endpoints carry client credentials, when a non-loopback `http://` token endpoint is configured, then deferred ID `13.2-RV4` is resolved by rejecting it with a sanitized, actionable error. `https://` token endpoints are accepted after the existing URL-shape checks pass; `http://` token endpoints are accepted only when the URI host is exactly `localhost`, `127.0.0.1`, or `[::1]`.

3. Given provider base URLs and OIDC token endpoints are operator-facing configuration, when transport policy is documented, then docs name the allowed schemes, local exceptions, production expectations, and secret-redaction guarantees.

4. Given validation errors can include endpoint text, when invalid transport is rejected, then tests assert no embedded credentials or token-like values leak in errors, logs, or snapshots.

## Tasks / Subtasks

- [x] Task 0 - Verify current transport validation and active deferred ID (AC: 1-4)
  - [x] Read `OidcTokenProvider.cs`, `EmbeddingProviderDefaults.cs`, `OidcTokenProviderTests.cs`, `EmbeddingProviderDefaultsTests.cs`, `docs/operations/embedding-providers.md`, and the `13.2-RV4` entry in `deferred-work.md` before editing.
  - [x] Confirm Stories 13.2, 13.7, 14.3, and 15.2 are not actively `in-progress` or `review`; if an adjacent OIDC/provider story is active, stop and record the exact status.
  - [x] Identify every validation path that can accept `OidcTokenEndpoint`: direct `IOidcTokenProvider` calls, `EmbeddingProviderDefaults.Validate(...)`, tenant configuration actor writes, migration target config validation, and integration fake-server setup.
  - [x] Preserve Story 14.3 protections: userinfo, query strings, fragments, transport failures, caller cancellation, and token/body redaction must not regress.

- [x] Task 1 - Define one explicit HTTP exception policy (AC: 1, 2)
  - [x] Choose the committed policy shape before coding. Preferred policy: `https://` for all production token endpoints; `http://` allowed only for literal loopback hosts (`localhost`, `127.0.0.1`, `[::1]`) and only where local/test configuration intentionally needs it.
  - [x] Implement the committed HTTP exception as a literal host allowlist: `localhost`, `127.0.0.1`, and `[::1]` only. Do not broaden this to the whole `127.0.0.0/8` range unless the story is explicitly corrected again before development.
  - [x] Do not allow private LAN, link-local, wildcard, DNS-alias, Docker/internal aliases, or public `http://` hosts as a "local" exception. Examples that must fail include `http://auth.tache.ai/token`, `http://10.0.0.5/token`, `http://172.16.0.5/token`, `http://192.168.1.20/token`, `http://169.254.169.254/token`, `http://host.docker.internal/token`, `http://localtest.me/token`, and `http://keycloak.internal/token` unless product/architecture explicitly chooses a different named allowlist.
  - [x] Keep loopback detection deterministic and testable. Prefer a small URI-host helper near the existing URL validation or reuse existing address-classification code only if it preserves literal-token-endpoint semantics. Do not use `Uri.IsLoopback` alone if it accepts broader host forms than this story allows.
  - [x] Apply the policy in both `EmbeddingProviderDefaults.ValidateOptionalHttpUrl(...)` for `OidcTokenEndpoint` and `OidcTokenProvider.ValidateAndCreateKey(...)` so invalid tenant state is rejected before persistence and direct provider calls also fail closed. Keep `BaseUrl` transport behavior separate unless implementation records why it intentionally changed.

- [x] Task 2 - Enforce sanitized rejection for insecure token endpoints (AC: 2, 4)
  - [x] Reject non-loopback `http://` token endpoints with an `ArgumentException` naming the relevant field/argument and explaining that production OIDC token endpoints require HTTPS.
  - [x] Reject invalid transport before any outbound HTTP request is built or sent. Tests must prove the scripted handler/substitute saw zero requests for non-loopback HTTP token endpoints.
  - [x] Do not echo the full URL in exception text. It can contain hostnames, path conventions, realm names, or accidentally embedded values even after Story 14.3 query/userinfo rejection. Error text may name the field/argument, scheme class, and sanitized policy cause, but must not include full token endpoint URLs, query strings, fragments, embedded credentials, client secrets, authorization headers, bearer-shaped strings, or JWT-like values.
  - [x] Preserve existing normalization in `OidcTokenProvider`: scheme/server/path only, trimmed client ID, trimmed scope, no negative caching, no change to per-key fetch collapse.
  - [x] Keep `BaseUrl` transport behavior separate from `OidcTokenEndpoint` unless the implementation explicitly records why provider base URLs should receive the same production HTTPS policy in this story.

- [x] Task 3 - Add focused unit coverage (AC: 1, 2, 4)
  - [x] Add `OidcTokenProviderTests` cases proving `http://localhost`, `http://127.0.0.1`, and `http://[::1]` token endpoints are accepted and normalized, without sending real network traffic beyond the scripted handler.
  - [x] Add `OidcTokenProviderTests` cases proving non-loopback `http://` token endpoints fail before the HTTP request is sent.
  - [x] Add `EmbeddingProviderDefaultsTests` cases proving OIDC config accepts literal loopback HTTP token endpoints where local/fake servers need them and rejects non-loopback HTTP token endpoints.
  - [x] Add negative tests for public hostnames, private IPv4, link-local metadata hosts, Docker/internal aliases, DNS aliases such as `localtest.me`, and `127.0.0.2` so the implementation cannot accidentally use a broader loopback/private-host helper.
  - [x] Assert rejection messages contain the parameter/field name and HTTPS/local-loopback guidance, but do not contain credential-looking substrings, query-like values, bearer tokens, or the complete endpoint URL.

- [x] Task 4 - Update operations guidance and deferred-work disposition (AC: 2-4)
  - [x] Update `docs/operations/embedding-providers.md` with the final transport policy for `OidcTokenEndpoint`: production uses HTTPS; local loopback HTTP is allowed for local Keycloak/fake-server development; non-loopback HTTP is rejected.
  - [x] Name the existing secret-redaction guarantees: tenant config stores only secret names, token responses are sanitized before preview/truncation, bearer-shaped values are redacted, and validation errors must not echo full unsafe URLs.
  - [x] Add a Story 15.4 rollup section to `_bmad-output/implementation-artifacts/deferred-work.md`.
  - [x] Mark `13.2-RV4` as `resolved`, `accepted`, or `carried-forward` using the Story 14.5 structured fields: `ID`, `Status`, `Source story`, `Target artifact`, `Re-open trigger`, and either `Evidence` or `Rationale`.
  - [x] Do not sweep adjacent IDs such as `13.2-RV7`, `13.2-RV8`, `13.2-RV9`, `13.4-RV2`, or provider registry IDs unless implementation genuinely resolves them and the story records why they became in scope.

- [x] Task 5 - Validate and record completion (AC: 1-4)
  - [x] Run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~OidcTokenProviderTests"`.
  - [x] Run `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingProviderDefaultsTests"`.
  - [x] If a shared URL helper changes or `UrlHostValidator` is reused, run its focused tests or add them under the matching server test folder.
  - [x] Run `dotnet build Hexalith.Memories.slnx` when the local SDK permits it.
  - [x] Run `git diff --check -- src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs src/Hexalith.Memories.Server/Ingestion/UrlHostValidator.cs tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs docs/operations/embedding-providers.md _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/15-4-token-endpoint-transport-policy.md`.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` - UPDATE. Direct token endpoint scheme/host validation, loopback HTTP exception, sanitized rejection, and normalization guardrails.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` - UPDATE. Tenant embedding config validation for `OidcTokenEndpoint` transport policy.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs` - UPDATE. Direct provider validation, loopback acceptance, non-loopback HTTP rejection, and no-request/no-leak assertions.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` - UPDATE. Tenant config transport policy coverage for local loopback and production rejection.
- `docs/operations/embedding-providers.md` - UPDATE. Operator-facing token endpoint transport policy, local exception, and redaction guarantee.
- `_bmad-output/implementation-artifacts/deferred-work.md` - UPDATE. Structured disposition for `13.2-RV4`.
- `_bmad-output/implementation-artifacts/15-4-token-endpoint-transport-policy.md` - UPDATE. Record implementation notes, validation, review findings, and file list.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through BMad workflow/status transitions.

Possible files only if analysis proves they are necessary:

- `src/Hexalith.Memories.Server/Ingestion/UrlHostValidator.cs` - UPDATE only if reusing or extending existing host-classification logic is cleaner than a token-endpoint-specific helper. Preserve SSRF behavior for content fetching.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/UrlHostValidatorTests.cs` - NEW/UPDATE only with matching focused coverage if `UrlHostValidator.cs` changes.
- `docs/dev/embedding-providers.md` - UPDATE only if developer-facing fake-server or local Keycloak behavior needs a short cross-reference.

Read/verify only:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md`
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md`
- `_bmad-output/implementation-artifacts/14-3-oidc-and-embedding-security-hardening.md`
- `_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md`
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingClient.cs`
- `tests/Hexalith.Memories.IntegrationTests/Fixtures/OllamaOidcFakeServer.cs`
- `src/Hexalith.Memories.AppHost/Program.cs`

Forbidden by default:

- `.github/**`
- `tools/MigrateEmbeddingVectors/**`
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs`
- `src/Hexalith.Memories.Server/Migration/**`
- `src/Hexalith.Memories.Server/Activities/**`
- `src/Hexalith.Memories.Server/Workflows/**`
- `src/Hexalith.Memories.Server/Actors/**`
- `tests/Hexalith.Memories.IntegrationTests/**`
- `Directory.Packages.props`
- `Directory.Build.props`
- `NuGet.config`
- `Hexalith.AI.Tools/**`
- `Hexalith.Commons/**`
- `Hexalith.EventStore/**`
- Any submodule pointer change

## Dev Notes

### Current Implementation State

`OidcTokenProvider.ValidateAndCreateKey(...)` currently accepts absolute `http://` and `https://` token endpoints. Story 14.3 already hardened this path so userinfo, query strings, and fragments are rejected without echoing embedded values. It then normalizes the cache key to scheme/server/path, trims client ID and scope, and leaves token acquisition to the detached `HttpClient` fetch path.

`EmbeddingProviderDefaults.ValidateOptionalHttpUrl(...)` also accepts absolute HTTP(S) URLs for both `BaseUrl` and `OidcTokenEndpoint`, then rejects userinfo, query strings, and fragments. Existing tests explicitly allow `http://localhost` and `http://127.0.0.1` for `OidcTokenEndpoint`, because local Keycloak and deterministic fake-server tests need loopback HTTP. Those local cases must continue to work.

The open risk is broader than URL shape: production OIDC client credentials should not be sent to a non-loopback `http://` token endpoint. `13.2-RV4` records that `http://` is accepted today with no TLS enforcement. This story should close that gap without breaking local developer and integration-test setup.

`UrlHostValidator` already has private/reserved address classification for content-fetch SSRF defense. Reuse it only if the semantics remain clear. For token endpoints, the local exception should be narrower than "private host allowed": loopback only is the expected safe exception.

### Recommended Policy

Use this policy unless code analysis proves a better repository-local shape:

- `https://` token endpoints are valid after existing absolute-URL, userinfo, query, and fragment checks pass.
- `http://localhost`, `http://127.0.0.1`, and `http://[::1]` token endpoints are valid for local development and fake-server tests.
- Other `http://` token endpoints are invalid by default, including public hostnames, private IPv4 ranges, link-local metadata ranges, Docker/internal aliases, DNS aliases that resolve to loopback, `0.0.0.0`, and other `127.0.0.0/8` literals such as `127.0.0.2`.
- The error should be actionable but sanitized: it should say production OIDC token endpoints require HTTPS and local HTTP is limited to loopback, without printing the full endpoint.
- The same effective policy should be enforced in the tenant/default config validation path and the direct token provider path. A shared helper or equivalent single policy is preferred so the two paths cannot drift.

| Token endpoint shape | Expected result | Notes |
|----------------------|-----------------|-------|
| `https://auth.example.test/token` | Accepted | Production/default path after URL-shape validation. |
| `http://localhost/token` | Accepted | Local development and fake-server loopback exception. |
| `http://127.0.0.1:8080/token` | Accepted | Literal IPv4 loopback exception. |
| `http://[::1]/token` | Accepted | Literal IPv6 loopback exception. |
| `http://auth.tache.ai/token` | Rejected | Public/non-loopback HTTP. |
| `http://10.0.0.5/token` / `http://172.16.0.5/token` / `http://192.168.1.20/token` | Rejected | Private network HTTP is not a local exception. |
| `http://169.254.169.254/token` | Rejected | Link-local/metadata HTTP is not a local exception. |
| `http://host.docker.internal/token` / `http://localtest.me/token` / `http://keycloak.internal/token` | Rejected | DNS aliases are not accepted as loopback policy. |
| `http://127.0.0.2/token` | Rejected | The allowed IPv4 literal is exactly `127.0.0.1`. |

### Deferred ID Targeted

This story is the normal lifecycle home for:

- `13.2-RV4`: `http://` token endpoint scheme accepted with no TLS enforcement.

Do not close this by documentation only unless implementation analysis proves there is no safe validation point. The expected resolution is code plus focused tests plus operator documentation plus a structured `deferred-work.md` disposition.

### Implementation Guardrails

- Preserve local Keycloak/fake-server loopback HTTP support. Do not require TLS for `localhost`, `127.0.0.1`, or `[::1]` test endpoints.
- Do not add a broad "allow insecure HTTP" configuration switch unless product/architecture explicitly requires it. A global switch is easy to leave on in production.
- Do not allow private-network HTTP endpoints as production substitutes for HTTPS. Client credentials still cross the wire.
- Do not echo full token endpoints in validation error text. Story 14.3 already rejected userinfo/query/fragment, but endpoint paths and realm names can still be sensitive.
- Preserve `OidcTokenProvider` cache, in-flight fetch, cancellation, timeout, and redaction behavior from Story 14.3.
- Keep provider base URL behavior separate unless explicitly changed and documented. This story targets token endpoint transport because it carries `client_secret`.
- Do not initialize or update nested submodules. Do not change root-level submodule pointers.

### Testing Requirements

Use xUnit, Shouldly, and the existing scripted HTTP handler patterns.

Minimum focused test additions:

- `OidcTokenProvider` accepts loopback HTTP token endpoints and sends exactly one scripted request.
- `OidcTokenProvider` rejects non-loopback HTTP token endpoints before any scripted request is sent.
- `EmbeddingProviderDefaults.Validate(...)` accepts OIDC loopback HTTP token endpoints.
- `EmbeddingProviderDefaults.Validate(...)` rejects OIDC non-loopback HTTP token endpoints.
- Rejection messages name the argument/field, include HTTPS/local-loopback guidance, and do not include the full endpoint, embedded credential-like strings, or bearer-shaped values.
- Direct-provider invalid-transport rejection happens before request construction/sending; the scripted handler records zero requests.

### Party-Mode Review - 2026-05-14

- Date/time: `2026-05-14T10:59:04+02:00`
- Selected story key: `15-4-token-endpoint-transport-policy`
- Command/skill invocation used: `/bmad-party-mode 15-4-token-endpoint-transport-policy; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - "Local/test context" was too ambiguous and could produce environment-flag or broad-private-network interpretations.
  - Direct provider validation and tenant/default config validation must enforce the same policy or unsafe tenant state can persist until runtime.
  - Literal loopback matching must not accidentally accept DNS aliases, private/link-local hosts, Docker aliases, or the whole `127.0.0.0/8` range.
  - Invalid transport must fail before outbound HTTP and error text must stay actionable without echoing the full endpoint or secret-like values.
- Changes applied:
  - Replaced vague local/test wording with a literal host allowlist for HTTP token endpoints.
  - Added a policy matrix, expanded negative examples, validation-boundary guidance, no-request-before-rejection evidence, and stricter redaction expectations.
- Findings deferred:
  - Whether future work should add an explicit insecure-HTTP override, private-network allowlist, or broader provider `BaseUrl` transport policy.
  - Whether shared URL policy should later be generalized beyond OIDC token endpoints.
- Final recommendation: `ready-for-dev`

- The review consensus kept the story scope narrow and implementable, but identified ambiguous "local/test context" wording as the main pre-dev risk.
- Clarified the concrete policy: HTTPS is the production/default path; HTTP is allowed only for literal `localhost`, `127.0.0.1`, and `[::1]`; private LAN, link-local, Docker/internal aliases, DNS aliases, public hosts, and broader `127.0.0.0/8` literals are rejected.
- Added explicit validation-boundary guidance requiring both tenant/default config validation and direct `OidcTokenProvider` validation, with a shared-helper or equivalent single-policy preference.
- Added rejection-timing and sanitization expectations: non-loopback HTTP must fail before any outbound HTTP request, and errors/logs/snapshots must not include full token endpoint URLs, query/fragment/userinfo, client secrets, authorization headers, bearer-shaped strings, or JWT-like values.
- Reconfirmed `BaseUrl` transport behavior, provider registry work, tenant contract reshaping, migration, integration fixture expansion, CI/release tooling, and submodule changes remain out of scope.

## Project Structure Notes

This is an OIDC transport validation and documentation story. The expected implementation stays inside ingestion URL validation, focused server tests, operations documentation, and deferred-work bookkeeping. Runtime embedding dispatch, tenant contract shape, migration tooling, integration fixture expansion, CI/release tooling, and submodules are out of scope.

## References

- `_bmad-output/planning-artifacts/epics.md` - Epic 15 and Story 15.4 acceptance criteria.
- `_bmad-output/implementation-artifacts/deferred-work.md` - target deferred ID `13.2-RV4`.
- `_bmad-output/implementation-artifacts/13-2-implement-oidc-token-provider.md` - original OIDC token provider behavior and deferred transport risk.
- `_bmad-output/implementation-artifacts/13-7-integration-tests-aspire-fixtures-and-operator-deployment-guide.md` - fake-server and local Keycloak context.
- `_bmad-output/implementation-artifacts/14-3-oidc-and-embedding-security-hardening.md` - current URL, transport, cancellation, and redaction hardening baseline.
- `_bmad-output/implementation-artifacts/15-2-provider-model-dimension-registry.md` - adjacent provider validation story; avoid overlapping provider/model registry work.
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs` - direct token endpoint validation and token acquisition.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs` - tenant embedding config URL validation.
- `src/Hexalith.Memories.Server/Ingestion/UrlHostValidator.cs` - existing host classification helper, if reuse is justified.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs` - token provider validation and scripted-handler tests.
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs` - tenant config validation tests.
- `docs/operations/embedding-providers.md` - operator-facing provider and Keycloak runbook.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Pre-dev hardening preflight JSON timestamp `2026-05-12T18:11:43Z` passed all checks with `working tree cleanliness` reporting `0 dirty paths`.
- Story selection chose `15-4-token-endpoint-transport-policy` because `ready_count` was `3`, below the target of `5`, and this was the first backlog story in sprint-status order.
- `/bmad-create-story 15-4-token-endpoint-transport-policy` context gathering loaded Epic 15 planning, sprint status, root project context, Stories 13.2, 13.7, 14.3, 15.2, and 15.3, current deferred-work entries, operations/developer embedding provider docs, current `OidcTokenProvider`, `EmbeddingProviderDefaults`, URL host validation, focused server tests, and recent git history.
- No external technology research was needed for this story. The implementation surface is repository-owned token endpoint validation, local fake-server compatibility, operator documentation, and deferred-work disposition.
- Dev-story activation confirmed adjacent Stories 13.2, 13.7, 14.3, and 15.2 are all `done`; no adjacent OIDC/provider story was active.
- Red phase: added non-loopback HTTP rejection tests first. The initial `EmbeddingProviderDefaultsTests` focused run failed 10 new cases because non-loopback HTTP token endpoints were still accepted. A parallel red-run also hit a transient source-link/build artifact file lock on the OIDC slice, so focused validation was rerun sequentially.
- Green/refactor phase: added `OidcTokenProvider.ValidateTokenEndpointTransport(...)`, reused it from `EmbeddingProviderDefaults.ValidateOptionalHttpUrl(...)` for `OidcTokenEndpoint`, kept `BaseUrl` transport behavior unchanged, and fixed IPv6 loopback handling after the first OIDC focused run rejected `http://[::1]/...`.
- Validation: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~OidcTokenProviderTests"` passed 44/44.
- Validation: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --filter "FullyQualifiedName~EmbeddingProviderDefaultsTests"` passed 156/156.
- Validation: `dotnet build Hexalith.Memories.slnx` passed with 0 warnings and 0 errors.
- Validation: `git diff --check -- ...` passed with only LF-to-CRLF working-copy warnings on touched files.
- Regression validation: `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` passed 1796/1796.
- Broader regression attempt: `dotnet test Hexalith.Memories.slnx --no-build` timed out after 15 minutes without usable result output; no completion claim is made for the full solution test lane.

### Completion Notes List

- Story context created on 2026-05-12.
- Scope is limited to OIDC token endpoint transport policy, local loopback HTTP exception, production HTTPS enforcement, sanitized validation failures, focused tests, operations guidance, and deferred-work disposition for `13.2-RV4`.
- Provider/model registry work, tenant contract shape, migration coordination, integration fixture expansion, CI/release tooling, and submodules are forbidden by default.
- No submodule state was touched.
- Implemented one explicit token endpoint transport policy: production OIDC token endpoints require HTTPS; HTTP is allowed only for literal `localhost`, `127.0.0.1`, and `[::1]`.
- Direct `IOidcTokenProvider` calls and `EmbeddingProviderDefaults.Validate(...)` now reject non-loopback HTTP token endpoints before outbound token requests or tenant config persistence.
- Added focused acceptance/rejection/no-leak/no-request tests for loopback HTTP, public/private/link-local/Docker/internal/DNS-alias HTTP, and `127.0.0.2`.
- Updated operator guidance and resolved `13.2-RV4` in the structured deferred-work register without sweeping adjacent deferred IDs.

### File List

- `_bmad-output/implementation-artifacts/15-4-token-endpoint-transport-policy.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/operations/embedding-providers.md`
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs`
- `src/Hexalith.Memories.Server/Ingestion/OidcTokenProvider.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs`

### Change Log

- 2026-05-12: Created Story 15.4 and promoted it from `backlog` to `ready-for-dev`.
- 2026-05-14: Party-mode review completed; tightened literal loopback HTTP policy, validation-boundary requirements, rejection timing, sanitization expectations, and test matrix while keeping status `ready-for-dev`.
- 2026-05-14: Implemented token endpoint transport policy, focused tests, operations guidance, and `13.2-RV4` deferred-work resolution; moved story to `review`.

## Story Completion Status

Implementation complete and ready for review. Status is `review`.
