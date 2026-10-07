# Fail-closed verification matrix

These are required **future tests**, not implemented or passing evidence.
The new focused lane is proposed in `implementation-tasks.md`.

For each negative case, assert: nonzero refusal / rejected result; no accepted
artifact published; no collector or downstream target/dependency invocation;
no source/config/tracker/A41/Production mutation; bounded secret-safe diagnosis.
Use a forbidden-dependency sentinel and output-directory inspection, not only
an exception assertion. Local bounded source/custody reads and the approved
authority-verification adapter are distinct from prohibited target dependencies.

| ID | Proposed test cases / input fault | Required predicate and proof |
| --- | --- | --- |
| N01 | `MissingExtraOrDuplicateGateRefuses`: 24/26 gates, duplicate gate identifiers, nonliteral names or missing C1.23/C1.24/C1.25. | Exactly C1.1–C1.25 individually attributable; no registration or launch. |
| N02 | `ReusedEvidenceRefuses`: shared accepted-gate/capture/disposition path or hash; distinct paths containing identical gate bytes. | Per-gate evidence cannot be reused; supporting source snapshots alone may be shared. |
| N03 | `ChangedBytesOrLengthRefuses`: alter capture, disposition, manifest, receipt, source support or cleanup after digest selection; race path replacement between check/read. | Hash/length/schema checks use one safe snapshot; complete reference chain and authority subject remain bound. |
| N04 | `MalformedOrSecretArtifactRefuses`: non-JSON artifact, wrong/unknown version, duplicate/unknown nested fields, incorrect types, BOM/invalid UTF-8, nonfinite value, over-budget size/depth/string; decoded or encoded credential/content canary. | Strict nested schema and decoded safety, no unsafe stream hash or diagnostic leakage. Reproduce the currently accepted non-JSON gap. |
| N05 | `CustodyEscapeRefuses`: absolute/traversal/backslash/aliased paths, leaf/ancestor symlink, special file, dangling or raced link, unapproved receipt origin/credential URI. | No read outside approved custody; safe traversal survives the race. |
| N06 | `UnregisteredProducerRefuses`: held/missing registry entry, arbitrary registered-looking producer, unrelated tracked `.editorconfig`/C2 file or wrong repository; unapproved verifier/registry revisions. | Exact approved implemented binding; Git validity does not create authority. |
| N07 | `IncompleteOrDirtySourceRefuses`: abbreviated/unrelated commit, omitted/extra helper or approved input, unsupported clean filter, source drift, mismatch between executed and normalized bytes, dirty capture or source final recheck failure. | Every used source/input is authenticated at authorized full commit and clean; normalization never conceals executed-byte changes. |
| N08 | `CommandLedgerForgeryRefuses`: wrong argument/legacy hash, missing/extra command, reordered required grammar, invalid interval/exit, null unsafe stream hash, zero or fabricated result counts. | Actual registered command grammar and meaningful target observations; local Git query counts cannot manufacture gate results. |
| N09 | `NeutralOrContradictoryEvidenceRefuses`: capture without disposition, rejected/needs-evidence disposition, blocked/empty observations, nonzero failures/skips, invented capture `gateStatus: passed`, record versus capture/source/command disagreement. | Expected neutral capture can be a review input, never acceptance alone; contradictions refuse rather than being normalized away. |
| N10 | `MixedScopeRefuses`: PG1/mixed profile/workload/gate/target/session, matching context label with another authenticated cluster, copied approval from a different capture. | Parse and compare every embedded scope fact plus authenticated actual target and decision subject. |
| N11 | `StaleOrClosedSessionRefuses`: old/future/out-of-order capture; review before completion; unrelated/revoked/expired grant; C1.16 closed-window artifacts relabelled to a new session; invalid or absent session. | Approved collection/review/use windows and one eligible session; archival acceptance cannot authorize. |
| N12 | `ForgedReviewerAuthorityRefuses`: distinct fabricated labels, unsigned/untrusted/altered receipt, wrong issuer/audience/decision, unauthorized role/delegation or wrong bound reasons. | Selected mechanism authenticates and authorizes all decision facts. Include the original label-only gap as a regression. |
| N13 | `SelfApprovalAndAliasIdentityRefuse`: producer equals reviewer; Operations/Security labels map to same principal; a review is valid for another gate/role/target/session. | True principal independence and exact scoped role authorization, not string inequality. |
| N14 | `ExpiredRevokedOrUnknownAuthorityRefuses`: expiry boundary, revocation before expiry, stale cached status, unavailable status provider or time, revoked role/key/session/decision. | Current independent approval validity and revocation; no availability fallback or lifetime inherited from capture freshness. |
| N15 | `MissingOrUnprovedCleanupRefuses`: absent receipt, failed/partial cleanup, foreign resource ownership, wrong baseline, unfinished job/lease, claimed read-only mode with mutating command. | Required cleanup proves owned resources restored and protected Production remains disabled. Attempted cleanup does not pass. |
| N16 | `ApprovalCycleOrSubstitutionRefuses`: gate approves its own dependent manifest; two aggregate approvals reused as three approval gates or post-evidence C5/C6; manifest changes after approval. | Approved acyclic P4 dependency and separately bound decisions; C1.23/C1.24/C1.25 stay separate. |
| N17 | `LegacyOrInspectionBypassRefuses`: unversioned predecessor, legacy `successors` shape, offline `--input`, unsupported version or freshness-disabled/historical-inspection result routed to launch/terminal authorization. | Every C1 consumer requires the same complete authenticated verdict before dependency calls; no compatibility fallback. |
| N18 | `RevocationBetweenAssemblyAndUseRefuses`: complete valid bundle at assembly, then revoke/expire/alter the receipt, grant, registry or artifact before consumption. | Revalidation on exact retained inputs immediately before downstream use; original `qualificationAuthorized: true` does not persist permission. |

Positive case `CompleteAuthenticatedInterchangeValidates` must exercise exactly
25 distinct capture/disposition/accepted artifacts, same eligible profile/session,
real semantic verifiers and the **selected adapter** under an isolated test
issuer/service, two different authorized bundle reviewers, positive target
observation counts, zero failures/skips and complete required cleanup. Show
that the capture remains neutral while its separately verified review passes.
Do not mock the final authority verdict to satisfy provider tests.

Until P5 supplies real gate implementations, a synthetic complete registry and
gate verifiers demonstrate assembly mechanics only. Mark that limitation in test
output and handoff; never call it complete operational C1 proof. Separate provider
and gate semantic tests are required before their bindings can be accepted.

For tenant/authorization scope tests, include mismatched session target/namespace
and any tenant scope authorized by a gate; prove refusal before dependencies.
Do not expose raw tenant/principal identities in the test evidence. The existing
product JWT/tenant guards remain a separate authority surface and cannot close
C1 reviewer authentication.
