---
title: 'Close five deferred embedding regression items'
type: 'chore'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-36, DW-47, DW-115, DW-135 and DW-707 as one bounded improvement to embedding regression reliability. Given rejected OIDC endpoints containing distinct sensitive URI segments, when validation or token acquisition fails, then diagnostics contain none of those segments and token acquisition sends no request. Replace constant-message vocabulary assertions with direct leak checks.

Given the search and embedding-conflict test fixtures, when constructing representative configurations, then use registered provider/model/dimension combinations and preserve the asserted search and conflict behavior. Given an Ollama 429 response with missing, ordinary or out-of-range Retry-After, when generating an embedding, then report the tenant and bounded retry delay after one request without refreshing credentials. Given Google and Ollama at 5000 requests/minute, when validating, then identify Google's rate-limit failure and Ollama's acceptance under an accurate test name.

Verify the existing nonparallel static-options collection and options restoration for the host-configuration test, then reconcile DW-707 with executed evidence. Resolve exactly these five entries with test evidence and reopen triggers. Independently review, commit and push to main as requested. Preserve runtime behavior, contracts, dependencies and the closed model registry.

</frozen-after-approval>

## Implementation Notes

- Small test-only route: no unresolved intent choices, runtime API, application topology change or irreversible implementation action. Commit/push to main is explicitly authorized. No service-backed verification is needed for mocked transport, pure validation and composition-registration tests.
- Baseline: clean main at 28a2beeb7e9baa1785b9cb70f2a8b750f071cfd7. Read root entry point, declared AI.Tools baseline/Git instructions, canonical project context, contributor file-scope/testing guidance, build/test/package configuration, EditorConfig/attributes, architecture D30/D31 and the previous embedding batch. Repository pins take precedence over stale context versions.
- Independent investigation confirmed DW-36, DW-47, DW-115 and DW-135. DW-707 already has Collection(EmbeddingProviderDefaultsStateCollection.Name), DisableParallelization=true and try/finally options restoration. Reconcile it after executing the existing test with the complete Server.Tests assembly. Leave accepted contract-boundary/model-expansion entries DW-45, DW-46 and DW-107 open.
- Code map: Ingestion/OidcTokenProviderTests.cs and Ingestion/EmbeddingProviderDefaultsTests.cs own the sanitization helpers and endpoint probes; preserve parameter assertions and loopback controls. The latter owns the mislabeled Google rate case. Ingestion/EmbeddingClientTests.cs supplies scripted HTTP/token helpers; reuse those for Ollama 429 cases. Search/HybridSearchServiceTests.cs owns MakeEmbeddingConfig; Endpoints/TenantEmbeddingConfigEndpointTests.cs owns the conflict payload fixture. EventStoreIntegration/EventIngestionWorkflowSchedulerAdapterTests.cs and Ingestion/EmbeddingProviderDefaultsStateCollection.cs are read/verify only for DW-707.
- Validate the individual Server.Tests project in Debug and execute the five changed classes plus existing host-configuration class directly. Follow with the complete Server.Tests assembly to verify static-options coexistence. Run line-ending, file-scope, tenant-evidence, review-readiness and whitespace checks. Validate the Conventional Commit with commitlint before and after committing; fetch and inspect divergence before pushing.

- Implemented all four test corrections in the five scoped C# files; DW-707 required ledger reconciliation only. Its existing isolation fix is 5b43fe2f8a0f04dc021921a077dff1a573c2ce5e. Added missing/ordinary/capped/zero/malformed Ollama retry-delay cases and distinct escaped/decoded URI segment leak probes. Validated representative conflict and search fixtures against the registry.
- `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --no-restore -m:1` passed before implementation (8.88 seconds) and after implementation (5.99 seconds), both with zero warnings/errors.
- `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*OidcTokenProviderTests' -class '*EmbeddingProviderDefaultsTests' -class '*EmbeddingClientTests' -class '*HybridSearchServiceTests' -class '*TenantEmbeddingConfigEndpointTests' -class '*EventIngestionWorkflowSchedulerAdapterTests'` passed 342 cases with zero failures/errors/skips/not-run (0.855 seconds).
- `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -q` passed four cases. Updated exactly the five selected ledger statuses with resolution evidence and reopen triggers. Complete-assembly validation and independent review follow.

- Complete verification: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` exited 0: total 3112, passed 3111, errors/failures 0, skipped 1, not-run 0 (58.991 seconds). The unchanged SubmoduleGuardTests.CheckSubmodulesTarget_FailsBuildWhenSubmoduleGitMarkerIsMissing is disabled because it renames a shared submodule .git marker and belongs in its dedicated manual/regression lane. The complete run verifies host-config/static-options coexistence for DW-707.
- `python3 tools/check-story-file-scope.py --story-key spec-five-deferred-embedding-regressions --changed-files-file /tmp/memories-embedding-changed.txt` passed all seven changed paths. The corresponding tenant-isolation check reports no changed surface; review-readiness reports no governed ledger/File List/evidence table and is a no-op. `git diff --check` passed. `node_modules/.bin/commitlint --edit /tmp/memories-embedding-commit.txt` passed before committing. `git fetch origin main` succeeded; `git rev-list --left-right --count HEAD...origin/main` returned 0/0.

- Independent blind review returned five concrete test-strength findings; all were verified and patched in scope. No findings are deferred. The final Debug build passed with zero warnings/errors (10.62 seconds), and the same focused six-class command passed 354 cases with zero failures/errors/skips/not-run. The 342-case focused and 3112-total full results above are pre-review evidence; final complete-assembly validation and independent fix confirmation follow.

- Final complete-assembly command exited 0 after review corrections: total 3124, passed 3123, errors/failures 0, skipped 1, not-run 0 (25.692 seconds). The same pre-existing submodule-mutation test is the only skip. Focused final runtime was 0.712 seconds for 354 cases.
- Independent reviewer confirmed all five original findings resolved and found no regressions. Line-ending tests (4/4), scoped-file validation (7/7), commitlint and `git diff --check` pass again after the corrections. No review findings were deferred. Runtime behavior, tenant routing, dependencies and submodules remain unchanged.

## Review Triage Log

- Medium, patched: combined URI probes stopped at user-info validation before exercising query, fragment or transport-policy diagnostics. Both suites now use independent HTTPS user-info/query/fragment and clean nonloopback HTTP rows, all retaining escaped/decoded path canaries.
- Medium, patched: exception-only canary assertions missed logger leakage. The token tests now capture the existing logger and exclude the full endpoint, every canary and the client secret from its entries.
- Low, patched: token rejection probes covered only GetAccessTokenAsync. The same four rows now cover InvalidateAndRefreshAsync too, with zero HTTP requests.
- Medium, patched: Ollama HTTP-date Retry-After mapping lacked dispatch-level coverage. Added past and far-future dates with day-long margins, expecting zero and the 3600-second cap, alongside existing delta/malformed cases.
- Low, patched: token assertions alone did not exclude redundant Dapr credential retrieval. Every 429 row now requires exactly one GetSecretAsync call as well as one token acquisition and no refresh.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.Server.Tests/Ingestion/OidcTokenProviderTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingClientTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantEmbeddingConfigEndpointTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-embedding-regressions.md`
