# Test Automation Summary - Story 22.6 (Post-Filter Recall)

- **Workflow:** `bmad-qa-generate-e2e-tests`
- **Date:** 2026-07-05
- **Story:** `_bmad-output/implementation-artifacts/22-6-post-filter-recall.md`
- **Framework detected:** xUnit v3 + Shouldly + NSubstitute; API/E2E coverage uses existing Aspire, Redis Stack, and FalkorDB integration fixtures. No new framework introduced.
- **Feature under test:** semantic metadata/source-type post-filter recall for plain semantic search, graph-scoped semantic search, and public `/api/search?axis=semantic` routing.

## Generated / Updated Tests

### API Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchApiIntegrationTests.cs` - added `GetSearch_WithSemanticAxisAndMetadataQueryBeyondInitialWindow_ShouldReturnLaterFilteredMatches`, which seeds two nearest semantic candidates that fail `metadataQuery` and two farther candidates that match, then exercises `/api/search?axis=semantic&metadataQuery=acme&maxResults=2`.
- [x] `tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchApiIntegrationTests.cs` - added `GetSearch_WithSemanticAxisAndSourceTypeBeyondInitialWindow_ShouldReturnLaterFilteredMatches`, proving public semantic `sourceType=url` does not silently lose farther URL matches behind nearer file matches.

### E2E / Integration Tests

- [x] `tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchIntegrationTests.cs` - existing story tests cover Redis-backed metadata recall and source-type recall beyond the initial KNN page window.
- [x] `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs` - existing story test covers graph-scoped semantic metadata recall inside the validated graph-approved key set.
- [x] Browser UI E2E is not applicable. Story 22.6 has no module UI.

### Unit / Service Tests

- [x] `tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs` - candidate-window expansion, `PAGINATION_LIMIT_EXCEEDED` preservation, service-side post-filter detection, source-type query-builder truthfulness, TAG escaping, and graph-scope key validation.

## Coverage

- API endpoints: `/api/search` semantic axis now covered for metadata and source-type post-filter recall through the public HTTP route.
- Plain semantic Redis path: metadata and source-type filters recover later matching candidates within the bounded `MaxCandidateWindow`.
- Graph-scoped semantic path: metadata-filtered recall is preserved inside the graph-approved `INKEYS` key set.
- Critical error cases: deep-pagination cap behavior and source-type fake pre-filter regression covered by unit tests.
- UI features: 0 applicable.

## Validation

- [x] `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -m:1 /nodeReuse:false --no-restore` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests -noLogo -parallel none -class "Hexalith.Memories.Server.Tests.Search.SemanticSearchServiceTests"` - 45 total, 0 failed.
- [ ] `DiffEngine_Disabled=true tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests -noLogo -parallel none -method "...SemanticSearchIntegrationTests.SearchAsync_MetadataFilterBeyondInitialWindow_ShouldReturnLaterFilteredMatches" -method "...SemanticSearchIntegrationTests.SearchAsync_SourceTypeFilterBeyondInitialWindow_ShouldReturnLaterFilteredMatches" -method "...GraphScopedSearchIntegrationTests.SearchAsync_GraphScopedSemantic_MetadataFilterBeyondInitialWindow_ShouldRecoverLaterMatches"` - blocked by Docker socket permission: Testcontainers failed to connect to `unix:///var/run/docker.sock`; inner error `System.Net.Sockets.SocketException: Permission denied`.
- [ ] `DiffEngine_Disabled=true tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests -noLogo -parallel none -method "...SemanticSearchApiIntegrationTests.GetSearch_WithSemanticAxisAndMetadataQueryBeyondInitialWindow_ShouldReturnLaterFilteredMatches" -method "...SemanticSearchApiIntegrationTests.GetSearch_WithSemanticAxisAndSourceTypeBeyondInitialWindow_ShouldReturnLaterFilteredMatches"` - blocked before test execution by sandbox infrastructure: Aspire backchannel bind failed with `System.Net.Sockets.SocketException (13): Permission denied`; Aspire then failed startup with `Container runtime 'docker' was found but appears to be unhealthy`.
- [x] `git diff --check -- tests/Hexalith.Memories.IntegrationTests/Search/SemanticSearchApiIntegrationTests.cs` - passed.
- [ ] `git diff --check` - blocked by existing story/worktree CRLF-as-trailing-whitespace debt in already-dirty files, including `_bmad-output/implementation-artifacts/sprint-status.yaml`, `src/Hexalith.Memories.Server/Search/SemanticSearchService.cs`, and `tests/Hexalith.Memories.Server.Tests/Search/SemanticSearchServiceTests.cs`. The file changed by this QA pass passes targeted diff-check.

## Checklist Result

- API tests generated/updated where applicable: pass; two public semantic API recall tests added.
- E2E tests generated where applicable: pass for backend/API integration intent; no browser UI exists.
- Tests use standard xUnit v3/Shouldly/NSubstitute APIs, cover happy path and critical error cases, have clear descriptions, use no hardcoded waits, and are independent through unique tenant/document ids: pass.
- Tests saved to appropriate directories and summary includes coverage metrics: pass.
- Integration execution remains blocked by sandbox Docker/Aspire permissions, not by compilation or test code.
