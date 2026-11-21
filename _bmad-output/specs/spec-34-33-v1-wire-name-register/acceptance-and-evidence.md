# Story 34.33 — Acceptance and evidence

## Selection and affected components

The selected active-foundation row begins “`Contracts.V1` has an owning component but no wire-name register file and no build-failing guard.” It binds AD-12 and AD-22. Epic 34 places 34.33 first and explicitly makes its completed register and guard a prerequisite of 34.1. In turn, 34.3 needs the source revision from accepted ingestion and 34.4 needs fenced writes before it can count acknowledgements. Other prerequisites of those later stories still apply. [Alignment ledger](../../planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md#current-alignment-gaps) · [Story 34.1](../../planning-artifacts/epics.md#story-341-accept-v1-ingestion-at-eventstore-first) · [Story 34.33](../../planning-artifacts/epics.md#story-3433-reserve-v1-wire-names-in-one-build-guard)

| Component | Work in 34.33 |
| --- | --- |
| `src/Hexalith.Memories.Contracts/` | Own the single tracked register. Cover current evidence-bearing V1 contracts, including the objects reachable from `EvidencePacket`. Identify fields by their containing V1 type and effective serialized name. Use `MemoriesJsonContext.Options`, attributes and converters to establish the actual JSON representation. |
| `tests/Hexalith.Memories.Contracts.Tests/V1/` | Put the guard and its controlled positive and negative fixtures in the existing Contracts test project. Compare the effective contract with the register. Discover fields independently of the register so adding an unlisted field is detectable. Compare old reservations with their reviewed predecessor to catch simultaneous changes on both sides. |
| Required Contracts build-validation lane | The existing CI unit-test selection includes this project. An incompatible edit must block the normal lane and identify the offending member or vocabulary. Existing serialization and CLI error-catalogue tests remain regression coverage. |
| File/URL intake, workflow scheduler and projection coordinator | Read these to establish the dependency. Their behavior changes in later stories. |

## Acceptance criteria

| ID | Given and when | Observable result |
| --- | --- | --- |
| AC1 | Run the guard against current V1 evidence types and the reviewed initial register. | Every in-scope field has one reservation per containing object. Each entry covers wire name, JSON kind and nested or element shape, nullable value versus optional presence, and property order. Include packet states, ingestion status values, the four AD-22 axis states, and the public V1 error-code catalogue. The baseline passes and removing a required vocabulary reservation fails the guard even when its producer has yet to ship. |
| AC2 | Exercise independent fixtures with an extra unregistered field, conflicting or reused name, incompatible JSON shape, changed nullability, changed omission rule, changed order, or unregistered, removed or reassigned closed value. | The guard rejects each case and reports the owning type or set, offending name/value and expected versus observed difference. Changing the register and DTO together to redefine an existing reservation also fails. |
| AC3 | Add a fresh optional field or closed value to a fixture, first alone and then with a maintainer-approved reservation in the same change. | The unregistered addition fails. The registered addition with an explicit version and field presence/null rules where applicable passes. Old reservations and previously valid payloads remain compatible. Exercise both a field addition and a value addition. |

The V1 packet states are `complete`, `partial`, `weak`, `empty`, `stale`, `degraded`, `unauthorized` and `pendingExpansion`. The ingestion status values are `queued`, `extracting`, `embedding`, `indexing`, `indexed` and `failed`.

For AC1, reserve the AD-22 values `available`, `unavailable`, `truncated` and `excluded`. Today's packet exposes `axesUsed` and `unavailableAxes`. Recording the reserved state vocabulary does not implement a new packet field. Likewise, record AD-12’s explicit-null rule and identify existing field-specific omissions pending 34.34. Validate those observed differences narrowly so another field cannot acquire the same omission silently. For errors, inventory public server-emitted V1 codes. The CLI translation catalogue includes synthetic transport codes, and `ErrorResponse.Code` alone reveals only a string shape. [AD-12 and AD-22](../../planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md#invariants--rules)

## Closure checks

Run from the repository root **after implementation**. The commands use the existing .NET test project and CI configuration. The implementation may choose the register filename and encoding. Record the actual path in the evidence.

```bash
contracts_tests=tests/Hexalith.Memories.Contracts.Tests/Hexalith.Memories.Contracts.Tests.csproj
git rev-parse HEAD
git ls-files -- src/Hexalith.Memories.Contracts
dotnet test --project "$contracts_tests" --configuration Release --list-tests
dotnet test --project "$contracts_tests" --configuration Release
```

1. **Baseline and coverage.** Identify the single tracked register in the file list. Establish that the discovered and executed tests include the live-contract guard, every AC2 case and both AC3 additions. Record test identities, counts and exit status. An empty discovery or a filtered-out guard supplies no evidence.
2. **Prove build failure.** In a disposable copy of the same implementation revision, change only the JSON name of `EvidencePacketAxisEvidence.Score` to an unregistered name such as `scoreDriftProbe`. Run the same Release test command and the normal required Contracts CI lane. Both must fail with the guard diagnostic naming the affected member and the old/new name. A compiler failure, an unrelated assertion or a negative fixture that merely expects rejection cannot stand in for this demonstration. Keep this deliberate edit out of the implementation.
3. **Prove compatibility.** Use the AC3 fixtures to capture rejection before registration and success after registration. Rerun on the clean implementation revision with the existing serialization tests. Preserve the register diff against its predecessor and the test output covering existing reservations.
4. **Record disposition.** Supply the implementation SHA, register path, commands, fixture changes, expected and actual diagnostics, clean pass and deliberate failure in a durable evidence path or CI run. The ledger owner, Jérôme Piquot, may issue a dated `confirmed resolved` for **this row** after rerunning the checks. While the tracker is frozen, retain the owed `sprint-status.yaml` entry, September 12 freeze reference and review date in that row. G6 and the other 51 active-foundation rows require their own evidence. [AD-20 and ledger](../../planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md#current-alignment-gaps) · [November freeze continuation](../../planning-artifacts/sprint-change-proposal-2026-11-20.md#artifact-edits-and-dependencies)

## Dev Notes

### Historical Context Classification

| Prior work | Classification | Permitted use |
| --- | --- | --- |
| September 12 proposed Epic 34 story reservations | `anti-template` | Problem inventory only. Use the approved 34.33 criteria for the scope. |
| Earlier 34.33 definition and 34.1 acceptance, 34.3/34.4 projection, 34.34 null emission, 33.8 packet bytes and 34.40 telemetry inventory | `historical-reference-only` | Establish the dependency and work boundaries. The approved 34.33 definition supplies the current criteria. |
| Existing packet and enum serialization tests | `current-narrow-pattern` | Reuse V1 serializer options and observable wire assertions. Their current coverage is narrower than a registry-wide compatibility gate. |

### Slice Proof

The single observable outcome is that Contracts maintainers can accept or reject a V1 change before merge. A clean baseline, a rejected edit and an approved addition test that same outcome. This planning record supplies no implementation or qualification evidence.

### Epic AC Verification

Checked on 2026-11-20 at repository commit `75c5b5f53af8ce122a42393d5ec7ff795d554df0` with working-tree spine SHA-256 `3c7bf368360f37c4d77d15ed9e9ee402660e767bbb9fbe85203f813215999f0e`. Rerun from the repository root with the following shell variables. The criteria above describe desired behavior.

```bash
planning=_bmad-output/planning-artifacts
spine="$planning/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md"
v1=src/Hexalith.Memories.Contracts/V1
cts=tests/Hexalith.Memories.Contracts.Tests
endpoints=src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs
```

| Epic claim | Class | Command / evidence | Observed | Verdict |
| --- | --- | --- | --- | --- |
| “Execution consumes the completed Story 34.33 register/guard outcome” | Dependency | `rg -n -F -e '34.33 → 34.1 → 34.8' -e 'Execution consumes the completed Story 34.33' "$planning/epics.md"` | Both the approved order and 34.1’s slice proof name the dependency. | `confirmed` |
| “Execution consumes Story 34.1's confirmed acceptance” | AD-3 prerequisite | `rg -n -F -e "Execution consumes Story 34.1's confirmed acceptance" -e "current-revision completion contract" "$planning/epics.md" "$planning/prd.md"` | 34.3 requires accepted source revisions. FR13 anchors three-axis completion to that accepted revision. | `confirmed` |
| “IngestionInput declares an optional IdempotencyToken property” and “UrlIngestionRequest has no IdempotencyToken declaration” | Transport | `rg -n IdempotencyToken "$v1/IngestionInput.cs" "$v1/UrlIngestionRequest.cs"` | Only generic input declares the nullable string. | `confirmed` |
| “IngestionEndpoints currently schedules an ingestion workflow” | Source behavior | `rg -n -F -e '.ScheduleAsync(candidateInstanceId' -e 'workflowScheduler.ScheduleAsync(workflowConfigurationCapture.NewUnitId()' "$endpoints"` | File and dedicated URL handlers both call the scheduler. | `confirmed` |
| “The name register is a single tracked file inside Hexalith.Memories.Contracts” | Adopted rule | `rg -n -F 'The name register is a single tracked file' "$spine"` | AD-12 assigns ownership, same-change registration and a build guard. | `confirmed` |
| “no wire-name register file and no build-failing guard” | Scoped absence | `git ls-files -- src/Hexalith.Memories.Contracts "$cts"` and `rg -ni -e 'wire.?name' -e 'name.?regist' -e 'wire.?shape' src/Hexalith.Memories.Contracts "$cts" tools .github/workflows/ci.yml` | File inventory, contract project and test/CI entry points show individual wire assertions and no shared tracked register or guard consuming one. | `confirmed` |
| “Contracts.V1 EvidencePacket is a current wire type” | Representation | `rg -n -e 'record EvidencePacket' -e 'enum EvidencePacketState' -e 'WhenWritingNull' -e 'UnavailableAxes' "$v1/EvidencePacket.cs"` and `rg -n -e 'Queued' -e 'Indexed' "$v1/MemoryUnitStatus.cs"` | Packet, state enum, status enum, nullable omissions and the legacy axis buckets exist. | `confirmed` |
| “error-code catalogue” is an AD-12 reservation | Vocabulary boundary | `rg -n -F 'error-code catalogue' "$spine"` and `rg -n -e 'record ErrorResponse' -e 'synthetic CLI transport codes' "$v1/ErrorResponse.cs" src/Hexalith.Memories.Cli/Errors/ErrorMessageCatalog.cs` | Contracts carries a string code. The CLI catalogue documents additional synthetic codes. | `confirmed` |
| Existing contract validation provides a build entry point | Test location | `rg -n -F 'unit-test-projects: tests/Hexalith.Memories.Contracts.Tests/' .github/workflows/ci.yml` and `rg -n -F 'MemoriesJsonContext.Options' "$cts/V1/EvidencePacketSerializationTests.cs"` | The required CI configuration selects Contracts.Tests, which already exercises the V1 serializer. | `confirmed` |
| “The successor sprint tracker remains on hold” | Release and tracking | `rg -n -F -e 'successor sprint tracker remains on hold' -e 'no-go' "$planning/prd.md"` and `rg -n -F 'tracker retains its existing freeze' "$planning/sprint-change-proposal-2026-11-20.md"` | Planning approval and the release verdict are recorded separately. | `confirmed` |
