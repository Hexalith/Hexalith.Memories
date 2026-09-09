# Mechanical validation

**Target:** `../architecture.md` (legacy architecture artifact)

**Validator:** `.agents/skills/bmad-architecture/scripts/lint_spine.py`, invoked through its `lint()` function because the legacy target is a flat `architecture.md` rather than a run folder containing `ARCHITECTURE-SPINE.md`.

## Result

- Validator status: failed (`ok: false`)
- Total findings: 6
- Severity: 6 low; 0 medium; 0 high
- Duplicate or non-monotonic `AD-n` identifiers: none detected
- Incomplete `AD-n` blocks: none detected because the document uses legacy `D1`–`D31` and `ADR-IDA-001` forms, not current `AD-n` blocks. This is a semantic/format-compatibility issue for the rubric review, not a mechanical pass.
- Unpinned `## Stack` entries: none detected; the document has no current-contract `## Stack` table.

## Findings

All six possible-template-token findings are false positives and should be ignored. They are intentional runtime key/index pattern variables, not authoring placeholders:

1. Line 75: `{tenant}` in the index naming scheme.
2. Line 75: `{model-version}` in the index naming scheme.
3. Line 300: `{tenant}` in the future concurrent-index naming scheme.
4. Line 300: `{model-version}` in the future concurrent-index naming scheme.
5. Line 697: `{tenant}` in a historical Redis key pattern.
6. Line 699: `{tenant}` in a Redis cleanup scan pattern.

## Disposition

- Ignore the six false positives.
- Discuss/update separately whether to migrate the legacy `D1`–`D31` registry into stable current-contract `AD-n` blocks carrying explicit `Binds`, `Prevents`, and `Rule` fields.
