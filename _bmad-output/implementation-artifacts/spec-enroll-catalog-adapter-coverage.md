---
title: Include the public catalog adapter in unchanged coverage enforcement
status: done
created: 2026-10-31
baseline_commit: 0a1f6bf94301094afd0045c4e396dad4dd613ce6
---

## Intent

CI41197988182 passed every actual unit/contract test, publisher fixture and other blocking job, then its unchanged coverage validator refused the new public EventStoreExtensions assembly because it had no declared source scope. Retained actual reports are `/tmp/memories0a-unit-artifact.zip` and `/tmp/memories0a-unit-coverage`. Enroll the assembly as required and declare its exact source directory; keep the76.5percent threshold, source/assembly exclusions, complete source discovery and all test/report requirements unchanged. This metadata correction changes no published production bytes or package versions.

## File Scope

Allowed files for this story:

- `tests/tooling/coverage_gate/line-coverage-gate.json`
- `tests/tooling/coverage_gate/coverage_contract_test.py`
- `_bmad-output/implementation-artifacts/spec-enroll-catalog-adapter-coverage.md`

## Verification

Run all existing coverage gate fixtures and validate the actual retained hosted coverage reports with the new scope. Require exact new-head full CI before parent release; do not claim scope enrollment itself proves coverage.

The initial existing fixture suite correctly caught its separate fixed required-assembly list. Extend only that list with the new required adapter assembly; preserve all threshold/source/report/exclusion assertions. The actual retained hosted coverage now validates:34438of42275 executable lines,81.46percent versus unchanged76.50percent minimum. Adapter assembly itself11of11lines,100percent. For local validation only, copied reports translate the exact GitHub runner repository prefix to this checkout; no filenames relative to that root, line numbers or hit counts change. Original artifact and7report hashes/mapping are retained in `/tmp/memories-adapter-coverage-path-remap.json`. Production validator remains unchanged.

Final existing coverage gate fixtures:36of36pass,zero failures. Actual hosted union remains81.46percent with the adapter required at100percent, preserving every existing guard. Exact new-head CI remains required by the parent; no production package republish is needed for this test-only metadata correction.
