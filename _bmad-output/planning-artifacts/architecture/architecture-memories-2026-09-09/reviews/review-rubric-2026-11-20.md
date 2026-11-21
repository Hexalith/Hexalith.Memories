# Architecture spine rubric review

**November 20, 2026 · Reviewed against Memories `75c5b5f5` and the refreshed direct dependencies.**

**Verdict at review:** Revise the recovery wording before handoff. The 23 decision IDs and the November release disposition are intact. Two smaller corrections affect gate interpretation.

| Priority | Finding | Required correction |
| --- | --- | --- |
| High | AD-2 and AD-16 prohibit operational backup/restore of shared Redis and FalkorDB projections. AD-3 attributes checkpoint invalidation to “AD-2's derived-store restore.” The Reliability entry and the checkpoint ledger row repeat this. A recovery implementation could interpret those passages as authorizing restoration from a shared projection snapshot. | Preserve the explicit `reprojectionRequired` state and monotonic source version. Tie invalidation to detected loss, inconsistency or reprojection during authoritative rebuild. Distinguish EventStore replay, same-live-tenant application import and Redis-input repair in the operational summary. [November recovery contract](../../../addendum.md) |
| Medium | The G3 summary uses “within 30 minutes,” which can admit a run of exactly 30 minutes. NFR31 sets a strict threshold and defines the cold-machine inputs. | Use “under 30 minutes” and refer to the PRD for the environment and timing protocol. [PRD measurable outcomes](../../../prd.md#measurable-outcomes) |
| Medium | The PostgreSQL ledger entry says to reopen DW-730 before enabling a lifecycle writer. The approved scope reset reopens the campaign only when a Production deployment path enables writes or runs a lifecycle replica. AD-17 already records that trigger. | Use the same observable trigger in the ledger and retain separate Production qualification requirements. [Approved scope reset](../../../sprint-change-proposal-2026-11-01.md) |

## Rubric coverage

- **Decision ownership and convergence.** AD-1, AD-2, AD-5 and AD-12 identify the owners of shared composition, domain mutations, authorization and the public contract. The native REST API and separately authenticated DomainService have distinct route families. The McpCli inventory dispositions and residual enrollment work are visible. [Native and SDK routes](../../../../../docs/operations/route-surface.md) · [Current readiness](../../../implementation-readiness.md)
- **Brownfield and evidence.** DW-732 covers logs, activity storage and display while the original URL still enters durable execution. DW-734 covers package-floor evaluation. Neither closure establishes full ingestion or source/package parity. The ledger retains accountable work for all-axis completion, isolation, partial-restore drills, test selection and the 100K-unit capacity claim. [November audit](../../../sprint-change-proposal-2026-11-20.md) · [Deferred work](../../../../implementation-artifacts/deferred-work.md)
- **Environment and operations.** The Stack distinguishes the repository-selected pins, checkout revisions, approved OpenBao inputs and measured deployment. AD-17 records the one-writer Development retention result, the Production operator control and the scope of accepted debt. Deployments, provider portability and recovery objectives have owners or explicit revisit conditions. [OpenBao observations](../../../../../docs/operations/openbao.md) · [Retention evidence](../../../../implementation-artifacts/tests/27-4-retention-verification-evidence.md)
- **Product scope.** The spine carries the missed checkpoint, the outstanding independent reviewers, the Administrator's reset authority and the unevaluated launch gates. Shipping qualification still depends on the PRD evidence protocol. [Release decision](../../../prd.md#release-decision-record)

## Final retest

**Pass. All three document findings are resolved.**

- AD-3 now records `reprojectionRequired` on detected loss or inconsistency before authoritative replay. The Reliability and checkpoint rows agree with AD-2 and AD-16 about recovery and erasure.
- AD-20 applies the strict G3 threshold of under 30 minutes, requires a clean NFR31 machine and includes Aspire installation and image pulls in elapsed time.
- AD-17 and the PostgreSQL ledger row now identify the same DW-730 trigger. A Production path must enable lifecycle writes or run a lifecycle replica to reopen the campaign.

The implementation and release evidence obligations in the ledger remain open. [Revised architecture spine](../ARCHITECTURE-SPINE.md)
