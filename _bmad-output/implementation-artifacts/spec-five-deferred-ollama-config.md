---
title: 'Close five deferred Ollama configuration regression gaps'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-108, DW-109, DW-110, DW-136 and DW-137 as one bounded correction to embedding configuration regression coverage. Given legacy Ollama JSON omitting authentication metadata, when the actor reads it, then absent BaseUrl returns Google defaults and a valid BaseUrl retains the legacy api-key configuration, without replacement writes or changing durable reindex state. Given empty or whitespace BaseUrl in stored configuration, when reading, then Google retains the stored value and Ollama returns Google defaults, with no replacement writes.

Given an unconfigured tenant and each independent combination of forceReindex and client ReindexRequired, when first writing Google or Ollama configuration, then store exactly one complete configuration with ReindexRequired false. Given qwen3-embedding:4b, when validating dimensions, then explicitly accept 2560 and reject other sampled dimensions with ParamName Dimensions. Correct the misleading Google-model test name to identify its actual supported Ollama model with an invalid dimension, and verify it fails for Dimensions.

Resolve exactly these five deferred entries using executed test evidence and reopen triggers. Independently review, commit and push on main as requested. Preserve the closed model registry and runtime behavior.

</frozen-after-approval>

## Implementation Notes

- Route: small test-only change with no unresolved intent choices, public API, dependency update or irreversible implementation action. User explicitly authorized commit/push on main. No application topology changes or service-backed verification are required by these isolated actor/default-validator tests.
- Baseline: clean main at aabf1ad1d42fb76f9f05b2ce0f4ac3dbbc4b3eff. Loaded root instructions, declared AI.Tools baseline and Git rules, canonical project context, contributor scope/testing guidance, EditorConfig, attributes, build/test/package configuration, relevant architecture constraints and Story 13.5 AC1/AC2. Repository configuration overrides stale contextual versions.
- Independent investigation confirmed all five gaps. Leave DW-46 and DW-107 open: legitimate same-provider model/dimension expansion needs a newly supported registry model, outside this test correction.
- Code map: tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs owns mock actor state, literal legacy JSON deserialization and first-write checks. Reuse StoredTenantEmbeddingConfig and MemoriesPersistenceJsonContext.Options; assert full configuration equality and no replacement writes. tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs owns registry validation cases and uses the existing static-options isolation collection. Only those two C# files are writable.
- Verification: build the individual Server.Tests project in Debug, execute its built assembly filtered to both changed classes, then run the complete assembly when the focused checks pass. Run the repository line-ending and file-scope checks and relevant commit hooks; validate the Conventional Commit with commitlint before and after committing. Record any broad blocker separately from focused passing evidence.

- Initial baseline build and both unchanged classes passed: 205 cases, zero failures/errors/skips. After implementation, `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Debug --no-restore -m:1` passed with zero warnings/errors (50.06 seconds).
- Focused verification: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class '*TenantConfigurationActorTests' -class '*EmbeddingProviderDefaultsTests'` passed 220 cases, zero failures/errors/skips/not-run (0.517 seconds). Fifteen additional cases cover eight actor read variants, six additional independent first-write combinations and explicit positive dimension validation.
- Updated exactly DW-108, DW-109, DW-110, DW-136 and DW-137 with resolution evidence and reopen triggers. The complete Server.Tests assembly, repository checks and independent blind review are in progress.

- Whole-project verification: `dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll` exited 0: total 3092, passed 3091, errors 0, failures 0, skipped 1, not run 0 (71.791 seconds). The existing disabled `SubmoduleGuardTests.CheckSubmodulesTarget_FailsBuildWhenSubmoduleGitMarkerIsMissing` explains that its behavioral probe renames a shared submodule .git marker and belongs in its dedicated manual/regression lane; it was not executed or changed here.
- Repository checks: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/line_endings -p '*_test.py' -q` passed 4 cases. `python3 tools/check-story-file-scope.py --story-key spec-five-deferred-ollama-config` with all four allowed changed-file arguments passed. Tenant-isolation evidence reports no affected surface; review-readiness reports no governed ledger and is a no-op. `git diff --check` and pre-commit-message `node_modules/.bin/commitlint --edit /tmp/memories-five-ollama-commit.txt` passed. `git fetch origin main` succeeded and divergence was 0/0.

- Independent blind review returned four concrete test-strength findings, all accepted for small in-scope corrections. Expanded legacy JSON to missing, empty, spaces, tabs and valid BaseUrl with both reindex flags; expanded Google blank-URL reads to both flags. First-write inputs now use distinct valid dimensions/rates/secret names and Ollama endpoint/OIDC metadata, compared against independently constructed complete stored records. Replaced the three new read theories' typed SetState-only checks with a read-only call allow-list covering every state-manager method and generic type. Used dotnet-inspect to inspect Dapr.Actors interfaces, confirming the actual resolved 1.18.10 version from project.assets.json after an initial lookup using the stale contextual 1.18.4 version; the installed tool requires `-v:q -s Methods` instead of the skill's older `--oneline` option.

- After the four review corrections, the same Debug build command passed with zero warnings/errors (3.28 seconds). The same focused two-class command passed 231 cases with zero failures/errors/skips/not-run (0.350 seconds), adding eleven further read cases. Ledger evidence now records this final focused count; earlier 220/3092 results describe the first implementation, before review corrections. Final whole-assembly verification and independent fix confirmation are running.

- Final full Server.Tests assembly command exited 0: total 3103, passed 3102, errors 0, failures 0, skipped 1, not run 0 (28.019 seconds). The same pre-existing shared-worktree submodule test remains disabled. The independent reviewer confirmed all four original findings are resolved and found no regression; its documentation correction identified the actual resolved Dapr.Actors version as 1.18.10, which was checked directly with dotnet-inspect. No findings were deferred.

## Review Triage Log

- Medium, patched: first-write factory-default inputs allowed provider-default replacement to pass. Both provider matrices now submit custom valid configuration and assert exact persisted metadata plus one state write.
- Medium, patched: blank Ollama URL tests only covered OIDC. Literal legacy JSON now combines absent authentication metadata with missing/empty/space/tab/valid BaseUrl and both reindex flags.
- Medium, patched: typed SetState assertions missed removal, alternative generic writes and other mutators. New read assertions allow only the interface's three read-only methods; any mutation or cache-changing call fails regardless of value type or state key.
- Medium, patched: legacy and Google blank-URL reads only used ReindexRequired true. Both flags now have complete returned-configuration equality checks.

- Low, patched: review caught the stale contextual Dapr.Actors 1.18.4 version in the investigation note. project.assets.json resolves 1.18.10; inspected that exact interface and corrected the note. Both versions expose the same three read-only methods used by the guard.

## File Scope

Allowed files for this story:

- `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Ingestion/EmbeddingProviderDefaultsTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-ollama-config.md`
