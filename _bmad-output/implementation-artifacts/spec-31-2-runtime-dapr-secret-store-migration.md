---
title: '31.2: Verify deployed Dapr secret resolution through OpenBao'
type: 'feature'
created: '2026-11-22'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
story_key: '31-2-runtime-dapr-secret-store-migration'
baseline_commit: '043fb04e46b97757c599b5851c42d634913474a5'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-31-context.md'
---

<frozen-after-approval reason="human-owned intent">

## Intent

Give operators reproducible evidence that the deployed `memories` app resolves its allowed credentials through Dapr's OpenBao-backed store, that the deployment enforces its access boundaries, and that every Kubernetes Secret left in the namespace has an accountable use or disposition.

## Boundaries & Constraints

Measure the existing `jpiquot@local` deployment in `hexalith-memories` using read-only probes. Retain TLS verification, bootstrap references, the runtime prefix and both component scopes. Inspect Secret names and types only. Keep Dapr tokens inside the pod and reduce responses there to key presence and HTTP status before emitting evidence. Exclude response bodies and credential values from every output. Reserve cluster changes, credential rotation, residue deletion and risk acceptance for their owners. Keep Epic 29 topology, Story 27.3’s verifier and Story 31.1’s platform record outside this change.

## I/O & Edge-Case Matrix

| Check | Required observation |
| --- | --- |
| Allowed name from running `memories` | Successful scoped read and expected field present. Record key name and status. |
| Same key from `memories-mcp`; disallowed or cross-prefix name from `memories` | Authorized sidecar request receives a store-scope or allow-list denial. Distinguish it from token errors, transport failure and an absent key. |
| Reserved `eventstore` scope | Verify scope membership, non-scoped denial and current workload inventory. A deployed app with that ID triggers a live read. |
| Runtime store cannot authenticate or load | Disposable failure test observes the component failure and absence of a successful read or fallback. |
| Cluster context unavailable | Record the failed preflight. Keep current inventory, drift and scoped-read checkpoints open. |

</frozen-after-approval>

## Code Map

- `deploy/kubernetes/base/dapr/{secretstore,config,statestore,pubsub,conversation-openai}.yaml` defines the store, policy and consumer routing. The access-telemetry store has its own bootstrap and prefix. Compare component declarations with both rendered overlays and the live component’s nonsecret fields.
- `deploy/kubernetes/base/service-accounts-rbac.yaml` names the grants. Reconcile them with pod env, volumes, image pulls, service accounts and Dapr bootstrap references. A component reference using `auth.secretStore` resolves through that named store.
- In `tests/Hexalith.Memories.Server.Tests`, reuse `NaturalLanguage/SubmoduleGuardTests.cs` for project scanning, the Deployment helpers for repository paths and rendered manifests, and `OpenBaoPlatformDocumentationTests.SecurityGuards.cs` for safe diagnostics. `src/Hexalith.Memories.Server/Ingestion/EmbeddingSecretStore.cs` is a Dapr consumer.
- `deferred-work.md` entries DW-726 and DW-727 record the September CR29/CR30 dispositions and their packet-verification limits. Story 29.2 migrated `deploy/dapr/components/secretstore.yaml`. Preserve those records and distinguish staged evidence from current deployed evidence.

## Tasks & Acceptance

- [x] In `Deployment/RuntimeSecretStoreMigrationTests.cs`, bind the Epic gate, local gate and Task 0 to exactly C1/C2/C3/C4a/C5a/C6. Require unique completed checkpoint rows. Mutation-test a missing checkpoint and a reinstated story-status gate.
- [x] Record the current context, UTC timestamps, namespace, app IDs, pod names, component metadata and names/types inventory in `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md`. Compare tracked and deployed component settings, then exercise the matrix. Attribute each denial to its actual control. Record ownership and a reopen trigger for drift.
- [x] Append the inventory in `docs/operations/openbao.md` using the legacy story's six columns and five classes. Reconcile the measured set and July baseline, both directions. Distinguish direct Redis/FalkorDB inputs, OpenBao keys, granted names and unused objects. Record residue disposition, owner, consequence and trigger without inventing acceptance.
- [x] Add guards for all product projects, Vault type and scopes in deployed artifacts, simulated bootstrap failure, and safe diagnostics. Check package references, provider clients, endpoint/configuration and credential patterns. Use synthetic canaries at observable output boundaries, including CLI, logging, telemetry and snapshots. Mutation-prove forbidden provider access and unsafe output with redacted failures. Scope the deployment guard to `deploy/kubernetes`.
- [x] Reconcile the parent story checkpoints and deferred-work references to attributable evidence. Record runner-derived method counts, mutation results, file list and outstanding blockers. Keep story and sprint status consistent.

Given a measured deployment, when operators replay the documented probes, then the matrix and per-Secret inventory substantiate AC1. Given product source and a synthetic secret, when the structural and output tests run, then provider coupling and value disclosure fail AC2 guards without disclosing the canary in their diagnostics.

## Implementation Notes

The base and both overlays retain the Vault runtime component and its two scopes. Production additionally removes the telemetry store from memories scope and removes the telemetry bootstrap grant. Story 29.2 already owns the migrated standalone template. The July inventory, current tracked routing and the 15:16Z live names/types are reconciled in the operations document. Deployed drift is recorded in the evidence with owners and reopen triggers.

Resume on 2026-11-22T15:14Z: in the Claude Code shell, `kubectl --context jpiquot@local -n hexalith-memories get pods -o name` listed the memories and memories-mcp pods using `~/.kube/config`. The 14:44Z preflight failure came from a shell without that kubeconfig. Run the documented read-only replay there and keep the failed preflight as history.

The replay completed at 15:23Z. Its first probe run exposed a BusyBox nc half-close that canceled permitted reads. The probe was fixed and regression-tested before the recorded reads.

## Spec Change Log

## Review Triage Log

The November 22 review covers the diff since `baseline_commit`. Gitlink changes from the user commit `2b82cb36` are excluded.

| Review finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind 1, Edge 2 and Edge claim 1: Dapr built-in `kubernetes` store | High | A 15:51:51Z metadata-only request listed `kubernetes` / `secretstores.kubernetes` in memories-675fd5d64-5zqpv beside both Vault stores and as the only store in memories-mcp-55d65d86d6-hh6mr. No Configuration scopes it, no workload disables it, and the memories Role grants get on nine names including `openbao-runtime-bootstrap`. Patch: record the path, reopen C2 and add an open owner task. Defer: the tracked-manifest and cluster remediation, which needs an owner decision on PG-ONPREM-2 inputs. |
| Blind 2: tasks complete while dispositions are open | Medium | All tasks are ticked and C2 reads complete, yet the residue disposition is pending and no open item tracks it. The spec `in-review` state is the workflow state and does not contradict the story. Patch: add an open owner subtask and set C2 to pending. |
| Blind 3 and Verification other 2: deferred-work notes | Low | The earlier dated note is history, and the note that follows supersedes it. The residue and drift items are tracked by the open story task from Blind 2. Rejected. |
| Blind 4, Edge 15 and Edge claim 2: no-SDK guard coverage | Medium | Server ships EventStore and every host ships ServiceDefaults, yet both are excluded. The root Directory.Build.props, Directory.Build.targets and Directory.Packages.props are never scanned. The tracked sources of both projects contain no provider match. Patch. |
| Blind 5, Edge 10 and Edge 12: fallback guard keyed on names | Medium | A new Component with a secretKeyRef and no `auth.secretStore`, or a new overlay, passes `KubernetesRuntimeStore_UsesVaultBootstrapTlsAndBothScopes`. Patch. |
| Blind 6, Verification other 1 and Edge 7: unreachable OpenBao | Medium | `fail_closed.py` always omits the bootstrap, yet Task 7 also claims the unreachable case. The daprd 1.19.1 versus 1.18.1 difference is disclosed, and that part is rejected as low. Patch. |
| Blind 7a, Verification gap and Edge 9: probe test runs in no lane | Medium | Pre-verified gap. Its fix edits `.github/workflows/ci.yml`, which Hexalith.CiCd owns under the module instructions. Defer. |
| Blind 7b: probe case count | Low | The runner reports one unittest method holding 13 subTests. The fix is a direct wording correction. Patch. |
| Blind 7c: discover pattern and BusyBox precondition | False | The default `test*.py` pattern matches `test_scope_probe.py`, and a missing BusyBox raises FileNotFoundError, which fails loudly. |
| Blind 8: untested probe rejection paths | Low | Wrong identity and Transfer-Encoding have no case. The fix adds handler variants for unlikely regressions. Rejected. |
| Blind 9: residue deletion preconditions | Medium | Only `redis-secret` was read. Deleting the other three objects could remove the only copy of a credential. Patch: require presence of the namesake OpenBao key first. |
| Blind 10: replay record | Low | Several recorded claims have no replay command, and Verification records kubectl v1.35.0 for the run that used v1.36.1. Patch. |
| Blind 11: method ledger | Low | The July Release net10.0 and November Debug net11.0 discoveries are not comparable, and the November rows lack a sorted method-set hash. Patch. |
| Blind 12: stale story sections | Low | Epic AC Verification still says no guard exists and that `deploy/dapr` uses the Kubernetes type. Expected File Ownership omits the new paths. Patch with inline dated notes. |
| Blind 13: requirement rewrite unrecorded | Low | The original C5 allowed a deferred-work re-disposition, and the spec Code Map directs the DW-726/DW-727 reconciliation. The rewrite itself is not recorded. Patch. |
| Blind 14 and Edge 17: telemetry assertion | Low | The handler only reads `Activity.Current` and writes no activity data. The audit event is checked with a count control. Object tags and StatusDescription are not serialized. Patch. |
| Blind 15: eventstore ServiceAccount grant | Medium | An unused ServiceAccount can get `openbao-runtime-bootstrap`, with no owner consequence or disposition. Patch. |
| Blind 16: Agent Model Used | Low | The resumed Claude Code run is missing. Patch. |
| Verification other 3: C1 linkage tool transport | Maybe-false | `tools/verify-access-telemetry-c1-linkage.ps1` uses the same half-closing nc pipe. It is parked under DW-730, and state-read cancellation is unverified. Defer, medium if true. |
| Edge 1: JSON-escaped canary | Medium | The credential contains a quote, so a raw JSON body echo would carry the escaped form past the leak assertion. Patch. |
| Edge 3: metadata without components | False | The probe exits 1 with registration unverified. That is a loud failure, never false evidence. |
| Edge 4: probe subprocess timeout | Low | Two 8 s requests can exceed 12 s. The fix is a direct correction. Patch. |
| Edge 5: fixture port race | Low | Unlikely, and the fix adds retry logic. Rejected. |
| Edge 6: undrained daprd pipe | Low | Failing startup output stays far below the pipe buffer, and the fix restructures capture. Rejected. |
| Edge 8: integration process timeout | False | `fail_closed.py` bounds the run with a 15 s deadline and `communicate(timeout=2)`. |
| Edge 11: default app ID | Low | Hypothetical, and the fix adds complexity. Rejected. |
| Edge 13: extra Vault metadata keys | Medium | `vaultTokenMountPath` or `caPath` would change authentication or trust while the guard passes. Patch: assert the exact key set. |
| Edge 14: case-insensitive token pattern | Low | Member access such as `b.ConfigureHttpClientDefaults` would fail the guard. The fix is direct. Patch. |
| Edge 16: deleted tracked file | Low | Cached `git ls-files` lists unstaged deletions, and Read then throws. Patch. |
| Edge 18: substitute HttpClient factory | False | No source configures header logging, so the default header redaction applies, and HttpClient logging never logs bodies. |
| Edge 19: gitlink rewind | False | The gitlinks moved in the user commit `2b82cb36`, after the story commits. That is not this change. |

## Verification

Build `tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj` in Debug using the pinned .NET 11 SDK and source references. Discover methods with `-list methods`, then execute the migration, platform documentation and production deployment classes directly with xUnit. Run the applicable CLI/telemetry tests, the tenant isolation evidence checker with the explicit changed-file list, and `git diff --check`. Keep current deployed results, disposable failure tests and archived CI results separately attributable.

The run record is `tests/31-2-runtime-secret-store-evidence.md`. C3/C4 and historical C5 have attributable evidence. C1 has current deployed evidence from 2026-11-22T15:16Z–15:23Z. C2 is pending. Dapr's built-in `kubernetes` secret store exposes granted Secrets outside OpenBao, and four residue Secrets await owner disposal or acceptance. Both are owner tasks, so story and sprint remain in progress.
