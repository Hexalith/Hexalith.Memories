---
title: 'Complete deferred test execution and verification reliability work'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
route: 'dispatch'
baseline_commit: 'a04830da13057f3d34293f8bcc38386485afec1f'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete ten eligible deferred items under test execution and verification reliability, using the confirmed checkout-deletion defect to select the parent. Work through test-runner safety and reporting, disposable topology readiness, and verification fixtures. Give each eligible item a dedicated implementer, a focused check, and a separate Conventional Commit with its DW ID. Evaluate blocked or obsolete candidates in priority order and record the reason.

## Boundaries & Constraints

Use disposable checkouts and test resources, preserve current test selection and assertions, and keep shared workflow changes with their owner. Verify every completion on the full solution and test inventory before recording closure and pushing main. Report any shortfall if the eligible sibling group is exhausted.

</frozen-after-approval>

## Code Map

- `tools/test.sh` and `tests/tooling/integration_fast_coverage/test_runner_test.py` own Bash result-directory cleanup, native exit reporting, and shell regression coverage.
- `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.Fast.cs` owns disposable deep-window Redis setup. Reuse the bounded handshake in `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs`.
- `src/Hexalith.Memories.AppHost/Program.cs` owns Redis endpoint rewrite and readiness, plus OpenBao generation initialization. `tests/Hexalith.Memories.Integration.NightlyTests/Ingestion/PipelinePersistenceIntegrationTests.cs` owns the restart proof.
- `tests/tooling/access_telemetry_c1/` holds the C1.16 subprocess verification. Preserve its refusal and output bounds.
- `tools/test-projects.*.txt` inventories the normal .NET lanes. The deferred register records eligibility and closure evidence.

## Tasks & Acceptance

- [x] DW-741. Given a results path that resolves to the checkout or leaves it, when the Bash runner prepares output, then it refuses before cleanup. Ordinary descendant result paths still work.
- [x] DW-742. Given an owned Redis container with an unavailable mapped port, when the deep-window fixture starts, then the wait is bounded and reports the failing phase and endpoint. The real Redis cases still execute.
- [x] DW-743. Given a native test failure with missing or partial reports, when the Bash runner exits, then it retains the native exit status and available report details. A successful run still requires its expected reports.
- [x] DW-744. Given a failed topology restart, when the fixture reports the original failure, then it includes bounded Redis and FalkorDB state, allocation, and loopback listener observations.
- [x] DW-745. Given concurrent dependents or a restarted Redis allocation, when the AppHost starts its consumers, then they await one probe for the current start and preserve independent cancellation.
- [x] DW-746. Given sequential C1.16 child commands inside individual deadlines, when their combined duration exceeds the former harness timeout, then the PowerShell fixture preserves the result and its existing process and output bounds.
- [x] DW-747. Given an Ollama fixture startup failure, when the owner tears down the topology, then the original failure also records the Redis allocation and loopback state observed before disposal.
- [x] DW-748. Given an OpenBao fixture startup failure, when the owner reports it, then the failure includes current OpenBao allocation and loopback listener evidence without disclosing seed or token values.
- [x] DW-749. Given an application-token-enabled topology, when a direct protected request carries a valid bearer and a missing or wrong application token, then the application denies it and accepts the matching positive control.
- [x] DW-750. Given an application-token-enabled topology, when the direct MCP integration client connects, then the client reaches its bearer and tool assertions using the fixture application token.
- [x] Review the remaining topology and verification candidates in priority order. Given a candidate whose prerequisite is absent or whose behavior is already covered, when its evidence is checked, then record the reason and advance to the next candidate.
- [x] Given verified implementations, when the complete build and per-project tests pass, then record the evidence and close the matching ledger entries.

## Implementation Notes

The numbered register has 159 open entries with existing skip notes. The 123 active flat entries have either an explicit open status or no status. Attended-build entries also exist as flat blocks. Assign the next sequential DW IDs to the selected flat entries, using explicit severity where present and ranking confirmed checkout loss and unbounded execution above missing diagnostics and coverage. Preserve ledger order among comparable entries.

- The Bash guard preserved valid nested TRX output. The unsafe-path regressions reached a cleanup tripwire on the baseline. The native-exit cases reproduced exit 37 becoming exit 1 before DW-743.
- DW-742 replaced the redundant Docker external-port readiness check with the existing Redis handshake under a bounded startup budget. Nine cases passed, including six with live Redis.
- DW-744 extends diagnostics after the prior restart ordering and teardown fixes. Both integration builds, 62 fixture cases, and a live nightly restart passed. DW-745 shared the Redis probe per generation. Four gate cases and a live Redis/MCP restart verified component rewrite after a prior generation completed.
- The C1.16 harness regression exceeded the former 12-second ceiling using nine valid child observations. Both focused cases passed, including the existing secret and subprocess bounds. DW-747 captured initial Redis evidence before teardown in 64 fixture cases and a live Ollama proof.
- The ordinary AppHost started and reached healthy before changes. The parked access-telemetry qualification suite reproduced the recorded owner-authority refusals and approved-profile input drift on the starting revision.
- DW-696 and DW-731 remain subject to hosted integration-lane ownership. The current replacement for the producer case named in DW-731 passed all four baseline variants, but that execution provides no hosted selection evidence.

- DW-748 captured OpenBao allocation and the loopback listener result before teardown. All 65 fixture cases and the live hybrid-search recovery case passed.
- DW-749 verified the protected memory read with missing, incorrect, and matching application tokens in enabled mode and confirmed default-mode access with a valid bearer. DW-750 reused the fixture-owned MCP client and passed all nine MCP cases in each token mode.

- Review repair commits bound Redis handshakes, preserved startup diagnostics, normalized Bash output aliases, and corrected readiness ordering and retries. The focused proofs passed 12 Redis cases, 66 diagnostic fixture cases, 18 Bash cases, five Redis gate cases, and the live Redis/MCP restart.
- The first full integration run exposed two owned containers that answered an internal Redis PING while rejecting connections on the mapped host port. The standalone FalkorDB and composite search fixtures now use bounded connection handshakes and replace one failed allocation. Their focused Release build and 32 live integration cases passed.

- The second full integration run passed 731 cases with four configured skips. Its nightly project passed 56 cases and failed two health tests after a replacement Aspire graph proxy accepted TCP without producing a PONG. The health test now restores the deliberately stopped FalkorDB resource in place. Both affected live tests passed together after the repair in 1f159ec53703283acc5e84f97696b4b68b695b14.
- The standalone C1.16 module passed all 21 cases. The broader C1 sweep exceeded its 20-minute watchdog. The parked access-telemetry lifecycle qualification reproduced its pre-existing approval and profile failures.
- The actual remote advanced to 81a5008fcc701b4bec09ef7378729c67128b4a0f while the first full checks were running. The workspace reflog records an update by push at 17:50 local. This session had issued no push command at that point and continued with separate fixes and the final gates.

- The third nightly run reproduced the same proxy symptom on Redis during a different full restart. The fixture now uses distinct direct loopback bindings for both owned backends. Commit d8a29c9d11fb405a77e2d7e7bbd7189c1227f786 passed all 67 fixture tests and four live cases covering both health checks, in-place backend recovery, and persisted ingestion across a full restart.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Disposition and evidence |
| --- | --- | --- |
| Blind 1 | High | Deferred. An explicit tracked descendant can still be selected for cleanup. A disposable source-placeholder test reproduced the pre-existing risk. |
| Blind 2 | Medium | Deferred. The pre-existing raw-path cleanup can follow a parent changed concurrently after containment validation. |
| Blind 3 | Low | Rejected. The coverage gate validates Cobertura content after the wrapper collects report files. |
| Blind 4 | High | Patched. The EventStore sidecars reference the shared Redis state and pub/sub files and now wait for their readiness. |
| Blind 5 | Medium | Patched. Live waiters now follow a superseding Redis start. |
| Blind 6 | Medium | Patched. Later consumers can share a recovery probe after an earlier probe fails. |
| Blind 7 | Medium | Patched. The live restart checks the rewritten component during the MCP startup callback. |
| Blind 8 | Medium | Patched. The remaining handshake budget and cancellation bound the actual wait. Late connections are disposed. |
| Blind 9 | High | Patched. Failure to write exception data cannot interrupt cleanup or replace startup failure. |
| Blind 10 | Medium | Patched. A failed endpoint observation leaves successful sibling observations available. |
| Blind 11 | Medium | Deferred. The pre-existing outer PowerShell watchdog leaves descendant reaping unproven. |
| Blind 12 | Medium | Existing deferred entry. The focused enabled-mode proof ran locally and hosted token selection remains separately owned. |
| Edge 1 | Medium | Patched. Redis is each relevant resource's final readiness wait after OpenBao or FalkorDB. |
| Edge 2 | Medium | Patched. The in-place Redis/MCP restart passed with the marker assertion at the startup boundary. |
| Edge 3 | Medium | Patched. Deadline and cancellation regressions verify late successful handshakes are closed. |
| Edge 4 | Medium | Patched. Type-only endpoint failures retain other observations. |
| Edge 5 | Medium | Deferred with Blind 2. Directory-descriptor anchoring is a separate cleanup change. |
| Edge 6 | Medium | Patched. A trailing dot on a valid results directory is normalized before cleanup. |
| Edge 7 | Medium | Patched. Unknown encodings report the malformed TRX without masking the native status. |
| Edge 8 | High | Patched with Blind 4. EventStore consumers use the current shared Redis gate. |
| Edge 9 | False | The original OpenBao entry names the Ollama embedding-provider failure fixture, which the diagnostic covers. |
| Verification 1 | Medium | Deferred. Helper faults and a live successful restart ran. A deliberately failed AppHost restart remains a separate fixture proof. |
| Verification 2 | Medium | Patched. The actual startup callback reads the rewrite marker. Gate tests also hold controlled readiness and verify caller behavior. |
| Verification 3 | Medium | Deferred. Listener failure and pre-cleanup attachment ran with controlled Aspire state. Forcing failure through the full DCP initialization boundary remains open. |
| Verification 4 | Medium | Existing deferred entry. Hosted enabled-mode enrollment belongs to Hexalith.CiCd. Both token modes passed locally. |

## Verification

- Build `Hexalith.Memories.slnx` with the full Release package profile.
- Execute every test project separately, including the complete integration assembly and nightly projects. Run the browser and repository tooling suites.
- Validate each commit message before and after commit.

### Final verification

- The complete Release solution build at d8a29c9d11fb405a77e2d7e7bbd7189c1227f786 passed with zero warnings and errors. All 12 .NET test projects passed. Nine unit and contract projects had 5,877 passes and one configured skip. Fast integration had 732 passes and four configured skips. Nightly integration passed all 58 tests. The benchmark project passed all 17 cases, and the independent benchmark run matched.
- Chromium passed all six tests and validated nine evidence artifacts. All 21 tests in the C1.16 module passed. Twenty of 22 supplementary tooling suites passed across the sweep and focused follow-ups. The publication follow-up completed the 39 remaining cases after 139 passes in the sweep. The runtime secret-store suite passed with the distribution's BusyBox staged in a disposable directory.
- The broader C1 tooling sweep reached its 20-minute watchdog. The separately parked lifecycle qualification reproduced the initial owner-authority refusals and approved-profile input drift. Those results remain in the verification logs.
- The deferred register closes DW-741 through DW-750. It retains 159 open numbered entries and 117 open legacy entries, including four follow-ups from review.
