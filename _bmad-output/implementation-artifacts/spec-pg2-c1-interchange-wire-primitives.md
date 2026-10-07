---
title: 'PG2 C1 interchange: bounded evidence snapshots and wire primitives'
type: 'feature'
created: '2026-10-07'
status: 'done'
route: 'oneshot'
baseline_commit: 'f4e7eb8626513c83f392a7cabf223b1a4673daa3'
review_loop_iteration: 1
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The user asked us to handle the technical prerequisites ourselves. Implement one separately scoped offline foundation for the proposed authenticated PG2 C1 interchange: strict bounded immutable JSON snapshots, canonical J1 semantic encoding, and exact reference validation/authentication. Reuse the engineering wire rules in the interchange proposal without selecting review authority, issuer, trust policy, session windows or accepting gate registrations.

Add `tools/access_telemetry_c1_interchange.py` and focused standard-library unittest coverage under `tests/tooling/access_telemetry_c1_interchange/`. Enforce 1 MiB artifact bytes, JSON depth 14, ordinary strings 4096 characters, paths 512, signed-64-bit integers and a 32 MiB aggregate unique-snapshot budget. Reject BOM/invalid UTF-8, duplicate decoded keys, invalid JSON, floats/nonfinite values, unpaired surrogates and excess budgets with bounded content-free errors. Hash and parse exactly the same retained bytes; make parsed trees immutable and preserve original bytes without normalization.

Validate `Ref` objects with exactly `path`, `sha256`, `byteLength`: canonical custody-relative POSIX spelling, no absolute/traversal/backslash/control or credential-shaped paths, exact lowercase 64-hex digest and positive signed-64-bit length within the artifact bound. Check digest and length against the retained snapshot. Lexical validation does not open files or prove custody/symlink safety. J1 uses sorted ASCII field names, preserved array order, literal Unicode and standard lowercase control escapes, with no float or Unicode normalization. Verify independent literal vectors with Python and available PowerShell; do not alter existing PowerShell argument hashing.

This implements common wire preparation only, not all of I1, any artifact-specific schema, authenticated acceptance, filesystem custody, successor ownership, downstream consumer migration or live qualification. Leave existing producers/validators, proposed authority decisions, story/sprint/A41 records, historical evidence and disabled Production configuration unchanged. No target/network/process is called by the new module and it publishes no approval or accepted artifact. Run focused edge/boundary tests and relevant existing tooling regression tests, review the change, and retain exact evidence. Do not stage, commit or publish without the already-required explicit post-review authority.

</frozen-after-approval>

## Implementation Notes

- The broader 27.4 draft remains conditional; the user's request authorizes autonomous reversible technical preparation, not independent acceptance or live fault/purge authority. This prerequisite has no story key or sprint transition.
- The interchange proposal is a source of engineering constraints only. No existing accepting authority adapter was found. Common parser preparation can proceed separately while P1-P7 remain unresolved.
- Before code changes, local Aspire start and resource inspection ran; this is a local startup baseline, not PG2 qualification. No AppHost/deployment changes are planned.
- Local startup, describe and stop all exited 0. All displayed local resources were Healthy; the EventStore process had Finished. Aspire was stopped before implementation and no resource or deployment was edited.
- Implemented immutable `JsonSnapshot`, per-validation `SnapshotReader`, `EvidenceReference`/`parse_ref`, `authenticate_snapshot` and `j1_bytes` in the new isolated module. The module performs no filesystem/network/process operation or publication. Reference paths are lexical only; it intentionally does not claim custody or reviewer authentication. Other artifact-specific field schemas and full I1 remain incomplete.
- Final focused discovery/execution: 32 unittest methods, all passed in 15.896 seconds with no failures/errors/skips. Subtests cover duplicate/escaped fields, malformed/secret diagnostics, size/depth/string/integer limits, immutable nested containers, exact byte/digest identity, 32 MiB aggregate refusal before parsing, canonical paths/Ref types, mismatched references, no dependency calls and bounded encoding allocation. Nine independently encoded PowerShell/Python literal vectors passed, including case-distinct fields, literal Unicode separators, root scalars/arrays/objects, escapes and both signed-64-bit extremes.
- Historical Context Classification: proposal common wire rules are current narrow engineering constraints, not an accepting contract; broad 27.3/27.4 and historical captures remain reference-only. Slice Proof: one offline wire-preparation outcome; no story/sprint registration, authority-provider selection, artifact-specific acceptance or target qualification is absorbed here.

- CI discovers the new fixtures unconditionally in `test-unit-contract`; the suite requires PowerShell and never substitutes a skipped cross-language comparison. Existing PowerShell producer argument hashing is unchanged.
- J1 output is capped while encoding, before allocating oversized serialized output. Reference validation rejects every Unicode Cc control and explicit credential-value aliases. The independent PowerShell oracle uses case-sensitive parsed dictionaries, ordinal key ordering and a handwritten encoder so that built-in serializer escaping cannot mask a wire mismatch.
- No source reference consumers were migrated. This foundation is complete; authenticated interchange and Story 27.4 remain incomplete. The existing, separately owned PostgreSQL SQL-contract CI registration omission is retained as a validation limitation rather than absorbed into this scope.

## File List

- `tools/access_telemetry_c1_interchange.py` — isolated strict wire primitives.
- `tests/tooling/access_telemetry_c1_interchange/test_wire_primitives.py` — boundary, denial, memory, immutability and independent cross-language coverage.
- `.github/workflows/ci.yml` — unconditional discovery of the new suite.
- `_bmad-output/implementation-artifacts/spec-pg2-c1-interchange-wire-primitives.md` — intent, implementation, review and validation record.
- `_bmad-output/implementation-artifacts/spec-27-4-retention-verification-operations-runbook-and-a41-close-out-7.md` — separate conditional story draft and a link to this completed prerequisite. No story/sprint/A41 transition.

## Review Triage Log

A context-free Blind Hunter reviewed the worktree. Other review layers were not configured for the rendered oneshot route. All six findings were verified, patched and checked again by the same reviewer; that independent rerun passed 32 tests with zero skips and found no remaining concrete defect in the fixes.

| Finding | Verdict / disposition | Evidence and correction |
| --- | --- | --- |
| B1 — oversized J1 output was built before refusal | medium / patch | An 8192-item array of 4096-character strings caused avoidable large allocation. Streaming JSON chunks now refuse before exceeding 1 MiB output; the regression requires peak allocation below 4 MiB and the independent reproduction measured approximately 1.24 MiB. This protects future tooling from oversized input. |
| B2 — non-ASCII control characters passed path validation | medium / patch | U+0085 and U+009B were accepted. All Unicode Cc characters now refuse, with explicit negative vectors. Such paths must not enter future custody lookup. |
| B3 — credential-shaped path aliases were accepted | medium / patch | Authorization and access/refresh/id-token value spellings were accepted. The lexical check now rejects those forms; tests retain content-free failure diagnostics. This prevents those tested credential values from entering retained reference paths. |
| B4 — PowerShell comparison lost case-distinct fields | medium / patch | A case-insensitive dictionary merged `A` and `a`. `ConvertFrom-Json -AsHashtable -NoEnumerate` and ordinal sorting now preserve both, with an independent literal vector. This restores useful cross-language verification for future developers. |
| B5 — PowerShell serialization escaped literal Unicode separators | medium / patch | The built-in serializer escaped U+0085/U+2028/U+2029 while J1 preserves Unicode. A handwritten independent encoder now agrees with literal expected bytes; escaped lookalikes are tested separately. This fixes a false interoperability oracle. |
| B6 — new tooling suite was absent from CI discovery | medium / patch | The new directory did not match existing discovery paths. An unconditional CI step now runs its complete suite; no allow-failure or skip condition was added. This prevents regressions in the new module from passing CI undetected. |

## Verification

Final evidence is retained at `/tmp/pg2-c1-wire-final-qyqhy9wi/manifest.json`, with exact argv, environment, baseline, exit codes, source hashes and separate logs. These are local reproducible test receipts, not accepted live evidence or authority receipts.

| Check | Result |
| --- | --- |
| Local Aspire startup, resource inspection and stop | Exit 0 for each; resources healthy; stopped before code changes. |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1_interchange -p "test_*.py" -v` | Exit 0; 32 passed; no failures/errors/skips. |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_lifecycle -p test_retention_verification.py -k test_c1_ -v` | Exit 0; 2 existing predecessor/freshness regressions passed. |
| `env PYTHONDONTWRITEBYTECODE=1 PYTHONHASHSEED=0 python3 -m unittest discover -s tests/tooling/access_telemetry_c1 -p pg2_runtime_control_plane_identity_test.py -k test_full_source_and_effective_invocation_and_child_receipts_are_recomputable -v` | Exit 0; 1 existing producer/hash regression passed. |
| `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true` | Exit 0; zero warnings/errors. |
| `git diff --check` and whitespace/line-ending inspection of the new files | Exit 0; Python/YAML LF and Markdown CRLF. |

The following existing CI inventory guard still fails, exit 1, one failure and zero skips:

```bash
env DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Cli.Tests/bin/Debug/net10.0/Hexalith.Memories.Cli.Tests.dll -method Hexalith.Memories.Cli.Tests.Ci.CiTestInventoryTests.CiWorkflow_RunsEveryToolingFixtureSuiteThatGuardsAShippedTool -parallelMode none -noLogo -failSkips -result-xml /tmp/pg2-c1-wire-final-qyqhy9wi/ci-inventory.xml
```

Exact failure: `tests/tooling/access_telemetry_c1_sql_contract is executed by no workflow step, so reverting anything it guards ships green.` This folder and omission exist at the baseline, independently of the new wire suite. The new directory is registered correctly. The existing deferral under “offline resumption review of spec-27-4-retention-verification-operations-runbook-and-a41-close-out-4.md (2026-10-07)”, V1, already assigns automated PostgreSQL-lane enrollment with its Docker/PowerShell/OpenSSL/pinned-image prerequisites to the Deployment Adapter Developer. That prerequisite must be completed before claiming green CI; the inventory guard was neither weakened nor bypassed. No live target or SQL harness was invoked in this prerequisite.

No index changes or commits were made. HEAD remains the recorded baseline. Story 27.4 remains `in-progress`, A41 remains open, and Production writes remain disabled. No approval, new session, live fault/purge operation, acceptance record or publication was produced.
