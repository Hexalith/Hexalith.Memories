---
title: 'Map restore target busy errors in the CLI catalog'
type: 'bugfix'
created: '2026-07-14'
status: 'done'
route: 'one-shot'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/26-2-backup-and-restore.md'
---

# Map restore target busy errors in the CLI catalog

## Intent

**Problem:** The restore target guard emits `RESTORE_TARGET_BUSY`, but the CLI error catalog does not translate it, and the restore stream lacks enough focused coverage to satisfy the line-coverage gate.

**Approach:** Add an actionable domain-error translation, pin its exit code and recovery wording, and exercise the staged Redis stream's success and failure boundaries.

## Acceptance

- `RESTORE_TARGET_BUSY` resolves to domain exit code `1`.
- The suggestion tells the operator to wait for the active restore before retrying.
- The catalog drift test reports no unmapped server codes.
- The staged Redis stream reads across chunks and fails closed for missing, truncated, or cancelled input.

## Verification

- Focused CLI catalog, stream, and drift tests pass.
- The repository line-coverage gate passes without lowering its threshold.

## File Scope

Allowed files for this story:

- `src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` - UPDATE. Translate the restore concurrency conflict.
- `tests/Hexalith.Memories.Cli.Tests/Cli/ErrorCatalogTests.cs` - UPDATE. Pin exit code and recovery wording.
- `tests/Hexalith.Memories.Server.Tests/Import/RedisChunkReadStreamTests.cs` - ADD. Exercise staged stream success and failure boundaries.
- `_bmad-output/implementation-artifacts/26-2-restore-target-busy-catalog.md` - ADD. Record the CI repairs and permitted change surface.
