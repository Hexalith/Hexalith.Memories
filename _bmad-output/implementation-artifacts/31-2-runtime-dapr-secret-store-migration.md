---
baseline_commit: 115d30b59101910d0fd30717f49a5fb7f1782547
creation_sprint_status_sha256: 6eb0077b6ee0306f1cc5f396d982849ab614aaee6138b882f4f693be5c25fd88
---

# Story 31.2: Runtime Dapr Secret-Store Migration to `hashicorp.vault`

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Blocking Activation Gate

**Do not start implementation until Story 31.1 is `done`.** Epic 31 states: *"Story 31.2 must not enter
implementation until Story 31.1 is `done`, so that the migration is evaluated against a documented platform
whose accepted limitations are already on record."*

At creation (2026-07-28) Story 31.1 is `in-progress`, gated on a Platform Operations `helm diff` and on
checkpoints C4/C5, which the approved 2026-07-28 sprint change keeps `not complete`. `ready-for-dev` here
means the specification is complete, **not** that the gate is open. Task 0 re-checks the gate and halts.

## Story

As an operator and security reviewer,
I want the runtime Dapr secret store migrated from Kubernetes Secrets to `hashicorp.vault`,
so that runtime secret resolution crosses one reviewed boundary and every remaining Kubernetes Secret is justified.

## Acceptance Criteria

1. **Given** the runtime `secretstore` component,
   **When** the migration completes,
   **Then** `deploy/kubernetes/base/dapr/secretstore.yaml` uses `hashicorp.vault` with the `eventstore` and `memories` scopes, the `memories` scope is proven by a live scoped read and the `eventstore` scope is proven structurally — its declared presence plus a demonstrated denial from a non-scoped app-id — because `eventstore` is a **reserved** scope with no deployed workload (**amended 2026-08-01** by approved Sprint Change Proposal 2026-08-01 (Story 31.2 reserved `eventstore` scope and deferred-work retarget), resolving Open Decision D1; reopen trigger: an `eventstore`-app-id workload is deployed, at which point the live read becomes both possible and required)
   **And** every remaining Kubernetes Secret is documented as an unavoidable OpenBao bootstrap credential or a direct pod input outside the DAPR secret-store boundary (NFR9).

2. **Given** secret-resolution behavior,
   **When** structural and integration tests run,
   **Then** no product project contains an OpenBao SDK, HTTP client, endpoint, or provider credential, and secret values are never exposed in logs, telemetry, CLI output, or test snapshots (NFR9, project-context "Never expose secrets").

## Tasks / Subtasks

- [ ] Task 0 - Verify the activation gate before any other work (blocking)
  - [ ] Re-read `sprint-status.yaml`. If `31-1-openbao-platform-hardening-and-documentation` is not `done`, **halt and report**. Do not start Task 1, do not edit any owned path, and do not set `review`.
  - [ ] Confirm Story 31.1's `done` conditions actually closed: the Platform Operations `helm diff` and checkpoints C4/C5. A status flipped without those is not an open gate.

- [ ] Task 1 - Re-measure the runtime secret boundary before changing anything (AC: 1)
  - [ ] Re-run every read-only probe in `### Measured Runtime State At Creation` against context `jpiquot@local`. The creation-time measurements are dated 2026-07-28 and are a starting point, never the evidence.
  - [ ] `kubectl get secret` is permitted for **names and types only**; never `-o yaml`, never `-o jsonpath` over `.data`. Never read `openbao-runtime-bootstrap`, `openbao-access-telemetry-bootstrap`, `openbao-seal`, `openbao-server-tls`, or `hexalith-keys-pki` contents. This constraint is inherited from Story 31.1 and is not relaxed.
  - [ ] Record, for every Kubernetes Secret that exists in namespace `hexalith-memories`, which tracked artifact references it and by what mechanism: Dapr component `secretKeyRef`, pod `env.valueFrom.secretKeyRef`, volume `secretName`, `imagePullSecrets`, RBAC `resourceNames` grant, or **nothing**.
  - [ ] A Secret reachable only through an RBAC grant, with no component, env, or volume reference, is **residue**, not a justified exception. Name it as such.

- [ ] Task 2 - Confirm or correct the migrated component; do not re-migrate what is already migrated (AC: 1)
  - [ ] `deploy/kubernetes/base/dapr/secretstore.yaml` **already** declares `secretstores.hashicorp.vault` with `scopes: [eventstore, memories]` as of commit `4d2e4e2f`, and `ProductionDeploymentArtifactsTests.ProductionOverlay_RendersExactSecurityPersistenceAndResourceContracts` already pins the type, `vaultAddr`, `skipVerify: "false"`, the `openbao-runtime-bootstrap` bootstrap reference, the `hexalith/memories/runtime` prefix, and **both** scopes at lines 184-191. Verify this against current source before writing a line. Do not claim a migration that commit `4d2e4e2f` performed; claim the proof.
  - [ ] If re-measurement shows the deployed component diverges from the tracked file, reconcile the file to the deployed component or record the divergence with an owner and a reopen trigger. Silence is not an option.
  - [ ] Do not weaken `skipVerify: "false"`, `tlsServerName`, the `caPem` reference, the `hexalith/memories/runtime` prefix, or either scope to make a check pass.

- [ ] Task 3 - Produce the per-Secret justification inventory (AC: 1)
  - [ ] Write the inventory into `docs/operations/openbao.md` under its existing `## Dapr secret boundaries` section (line 334 at creation), or a new operations document. Do **not** rewrite Story 31.1's platform sections; that file is jointly owned from this point and Story 31.1's measured-platform content is frozen.
  - [ ] One row per Kubernetes Secret with the exact header `| Secret | Namespace | Referenced by | Justification class | Owner | Reopen trigger |`. Justification class is exactly one of `openbao-bootstrap`, `direct-pod-input`, `story-27.3-retained`, `platform-31.1`, or `residue`.
  - [ ] `residue` rows are not a justification. Each needs a disposition: delete the Secret, drop the RBAC grant, or record an accepted blocker with owner, consequence, and reopen trigger. AC1 admits exactly two justified classes; anything else must be dispositioned or explicitly accepted.
  - [ ] Cover, at minimum, every Secret in `### Measured Runtime State At Creation`. Do not silently drop one because it is inconvenient.
  - [ ] Reconcile the inventory against `deploy/kubernetes/base/service-accounts-rbac.yaml`. `memories-dapr-secret-reader` currently grants `get` on nine Secret names while `memories-config` routes four of them through the OpenBao-backed `secretstore` instead — that gap is the least-privilege residue this AC exists to surface.
  - [ ] Do not describe the OpenBao prefixes as tenant partitions. They are shared **application** scopes; architecture D31 and Story 31.1 both say so.

- [ ] Task 4 - Prove both scopes by a live scoped read, and prove the denial (AC: 1)
  - [ ] Positive: from a running `memories` pod, resolve an allow-listed secret through the Dapr Secrets API against store `secretstore` and record that it **succeeded**. Record the HTTP status and the resolved key name only. **Never record, log, echo, or store the value.**
  - [ ] Negative: prove `memories-mcp` cannot read the same store. It carries app-id `memories-mcp`, which is in neither scope, and `memories-config` sets `defaultAccess: deny`. A denial that is not demonstrated is not evidence.
  - [ ] Negative: prove a secret name outside `memories-config`'s `allowedSecrets` list is denied for `memories` itself, so the allow-list is shown to be load-bearing rather than decorative.
  - [ ] **`eventstore` is a reserved scope: prove it structurally, never by a fabricated read.** Open Decision D1 was resolved 2026-08-01 — no pod carries app-id `eventstore` **and no manifest in `deploy/kubernetes/` declares one**; EventStore is linked as a library, not deployed as a Dapr app. Prove the scope two ways: (i) its declared presence in the component, bound by a `-method` selector, and (ii) a demonstrated denial from a non-scoped app-id, which the `memories-mcp` negative above already produces. Record explicitly that no `eventstore` workload is deployed, with owner `Memories Maintainer` and the reopen trigger "an `eventstore`-app-id workload is deployed". **Do not fabricate a read for an app-id that does not run**, do not stand up a throwaway `eventstore` sidecar, and do not describe the structural proof as a live read.
  - [ ] Record every command, UTC timestamp, cluster context, namespace, and pod name in `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md`.

- [ ] Task 5 - Add the structural no-SDK proof (AC: 2)
  - [ ] Add an executable structural guard asserting that no **product** project contains an OpenBao SDK reference, HTTP client construction, OpenBao endpoint literal, or provider credential. Product projects per architecture D30: `Hexalith.Memories.Server`, `.Cli`, `.Mcp`, `.Web`, `.Client.Rest`. Also cover `.Contracts`, `.AccessTelemetry`, `.AccessTelemetry.Clock`, `.AccessTelemetry.Contracts`, and `.Telemetry`.
  - [ ] Boundary projects `AppHost`, `Aspire`, `ServiceDefaults`, `Redis`, `EventStore` are **excluded by design** — `src/Hexalith.Memories.AppHost/OpenBao*.cs` is Story 29.1's sanctioned topology code and must not be flagged. Encode the exclusion as a named allow-list with a comment, not as a silently narrow scan path.
  - [ ] Follow the source-scanning style of `tests/Hexalith.Memories.Server.Tests/NaturalLanguage/SubmoduleGuardTests.cs` (repo-root location, `.csproj` XML introspection, regex over tracked sources) and the repo-file helpers in `Deployment/AppHostOpenBaoConfigurationTests.cs`. Do not invent a third mechanism.
  - [ ] Prove the guard is not vacuous: temporarily introduce a forbidden reference into one product project, confirm the assertion fails, revert, and confirm the file is byte-identical. Record each mutation.

- [ ] Task 6 - Add the secret-safety negative proof (AC: 2)
  - [ ] Assert negatively that a resolved secret value cannot reach logs, telemetry, CLI output, or a test snapshot. Reuse Story 31.1's proven pattern in `OpenBaoPlatformDocumentationTests`: PEM markers, OpenBao token prefixes `hvs.`/`hvb.`/`hvr.`/`s.`/`b.`/`r.`, `bao operator init` dump labels (`Unseal Key`, `Recovery Key`, `Initial Root Token`), and unlabelled long base64/hex runs — including `/` and `=` in the base64 class, which that guard originally omitted so roughly half of real key material split into sub-threshold runs and was invisible.
  - [ ] Scope the scan to **this story's** evidence artifact and any documentation it adds. Do not re-scan Story 31.1's records; that is its guard's job and duplicating it creates two divergent allow-lists.
  - [ ] Prove this guard is not vacuous with mutations, exactly as Task 5 requires.

- [ ] Task 7 - Adversarial cutover proof: the migrated store fails closed (AC: 1, 2)
  - [ ] Required by the remediation runtime checklist, Category 4 — see `### Remediation Runtime Checklist Applicability`. Prove that when the `hashicorp.vault` secret store cannot authenticate — the `openbao-runtime-bootstrap` Secret absent, or OpenBao unreachable — the failure is an **observable, fail-closed** component-load failure, and **never** a silent fallback to a Kubernetes-Secret-backed store or an empty-but-successful read.
  - [ ] This defect class is not hypothetical here: `DW 27.3-CR17` — cited here purely as provenance for the observed behavior, not as this story's gate — records `daprd` exiting with `failed to load components: rpc error: code = Unknown desc = Secret "openbao-runtime-bootstrap" not found`, which is the correct fail-closed behavior observed by accident. Turn the accident into an asserted contract.
  - [ ] Assert that no Kubernetes-backed secret-store component named `secretstore` can be reintroduced **into `deploy/kubernetes/**`** by a manifest edit without failing a test.
  - [ ] **Scope the assertion to `deploy/kubernetes/**` deliberately, and say why in a comment.** `deploy/dapr/components/secretstore.yaml` — the standalone/local Dapr template — still declares `type: secretstores.kubernetes` under the same component name and the same two scopes. It is Epic 29 Story 29.2 scope (`backlog`), not this story's. A repo-wide assertion fails on it immediately; do not "fix" it by editing Epic 29's file, and do not delete the assertion to get green. Name the file in the inventory as a known non-migrated standalone template with Story 29.2 as owner.

- [ ] Task 8 - Discharge `DW 27.3-CR29` and `DW 27.3-CR30` (`done` gate, not an AC)
  - [ ] **Retargeted 2026-08-01** by approved Sprint Change Proposal 2026-08-01, resolving Open Decision D2. This task previously named `DW 27.3-CR17`. That entry was **split on 2026-07-29** by Story 27.3's chunk-2 code review — one day after this story was created — and now covers **only** the Story 27.3 / checkpoint C2 arm. Do not chase it. Both entries below are `open`, both name Story 31.2 as owner, and both fire "before Story 31.2 is set `done`". Neither is an acceptance criterion — do not smuggle either into AC1 or AC2.
  - [ ] `DW 27.3-CR29` — *the OpenBao runtime secret store is still unproven by any executed lane.* Discharged when an executed lane loads `secretstore` **and** `access-telemetry-secrets` at their production `secretstores.hashicorp.vault` type against a **reachable OpenBao** and resolves their consumers' secretKeyRefs. The live cluster `jpiquot@local` is such a lane: it has a reachable OpenBao and `memories-*` pods run `2/2`, so `daprd` loaded the vault-typed component. **This folds into Task 4** — but a loaded component is not a resolved secret, so the discharge needs Task 4's executed secretKeyRef resolution, not merely a running pod.
  - [ ] `DW 27.3-CR30` — *no lane exercises the vault secret-resolution path at runtime.* AC8's static lane asserts the vault type against the rendered manifests while AC6's runtime lane guarantees that type is never the one running; the two are individually correct and jointly leave the path unexercised. Discharged when one lane loads a `secretstores.hashicorp.vault` component and resolves a secretKeyRef through it. Add the assertion that ties the two lanes together.
  - [ ] **The disposable `kind` lane is out of scope and already repaired.** The Administrator decided on 2026-07-28 to substitute verification-scoped `secretstores.kubernetes` stores in that lane, with a mandatory `secret-store-substitution.json` disclosure that `tools/validate-production-deployment-evidence.ps1` fails closed on; commits `564d5d56` and `64434e57`, owned by Story 27.3. **Do not repeat it, and do not edit `tools/verify-production-deployment.ps1`** — it is cross-pinned by `CiTestInventoryTests` (by source text) and by the `publish_containers` renderer path, and it is no longer in this story's owned-path set.
  - [ ] Record the outcome against **both** entries in `deferred-work.md` with re-derived evidence. If either cannot be discharged, re-disposition it with owner, consequence, and reopen trigger — do not leave it silently open while setting `done`.

- [ ] Task 9 - Validate the slice and record the phase truthfully (AC: 1, 2)
  - [ ] Build and run the focused lane recorded under `### Testing Baseline and Planned Delta`, then re-run `ProductionDeploymentArtifactsTests` and `OpenBaoPlatformDocumentationTests` as regression evidence — both read files this story may touch.
  - [ ] Run `git diff --check`, reconcile the cumulative File List against `baseline_commit`, and append a `dev-story` Change Log row with runner-derived actual deltas. Do not rewrite the create-story row and do not count planned tests as actual.
  - [ ] Fill every checkpoint row's review state and completion state from executed evidence. A row whose evidence did not run stays `pending` / `not complete` with a named blocker.
  - [ ] Run `python3 tools/check-tenant-isolation-evidence.py --changed-files-file <changed>` and record its output verbatim. Do not use the bare `--story-key` form of `check-story-file-scope.py` as scope evidence: on an unstaged worktree it exits `0` as a no-op, and this story declares no `## File Scope` section, so it reports "no parseable File Scope section" and passes vacuously.

## Implementation Checkpoints

Every row is mandatory. Each carries an accountable owner, an exact evidence command or artifact, a review state, and a completion state. Rows C1-C4 are the four required by Epic 31's Implementation-evidence clause. C5 is this story's `done` gate for `DW 27.3-CR29` and `DW 27.3-CR30` and is not an acceptance criterion (**retargeted 2026-08-01** by approved Sprint Change Proposal 2026-08-01; this sentence previously named `DW 27.3-CR17`, which was split on 2026-07-29 and whose remaining arm belongs to Story 27.3).

| Checkpoint | Accountable owner | Required evidence artifact and command | Review state | Completion state | Completion date |
| :--------- | :---------------- | :------------------------------------- | :----------- | :--------------- | :-------------- |
| C1 - Migrated component with the `memories` scope proven by a live scoped read and the reserved `eventstore` scope proven structurally | Memories Maintainer + Hexalith Platform Operations (`jpiquot`) | The tracked `deploy/kubernetes/base/dapr/secretstore.yaml` bound by its own `-method` selector, plus the executed live read, denial, and allow-list transcripts in `31-2-runtime-secret-store-evidence.md` with commands, UTC timestamps, context, namespace, and pod names. **Two proof modes, amended 2026-08-01 by approved Sprint Change Proposal 2026-08-01 resolving Open Decision D1:** `memories` by an executed live scoped read; `eventstore` by its declared presence in the component plus a demonstrated denial from a non-scoped app-id, recorded as a **reserved** scope with owner `Memories Maintainer` and the reopen trigger "an `eventstore`-app-id workload is deployed". A fabricated read for `eventstore`, or describing the structural proof as a live read, fails this row | pending | not complete | — |
| C2 - Per-Secret justification inventory for every remaining Kubernetes Secret | Memories Maintainer + security reviewer | The inventory table with the exact header in Task 3, bound by its own `-method` selector asserting the header, one row per measured Secret, a justification class from the closed set, and no empty or placeholder cell. Reconciled against `deploy/kubernetes/base/service-accounts-rbac.yaml` | pending | not complete | — |
| C3 - Structural no-SDK proof | Memories Maintainer | The Task 5 structural guard with its own `-method` selector, plus the recorded not-vacuous mutations | pending | not complete | — |
| C4 - Negative proof that a secret value cannot reach logs, telemetry, CLI output, or a snapshot | Memories Maintainer + security reviewer | The Task 6 negative guard with its own `-method` selector, plus the recorded not-vacuous mutations | pending | not complete | — |
| C5 - `DW 27.3-CR29` and `DW 27.3-CR30` discharged or re-dispositioned (`done` gate; not an AC) | Story 31.2 owner + Story 27.3 register owner | **Retargeted 2026-08-01 by approved Sprint Change Proposal 2026-08-01 resolving Open Decision D2**; this row previously named `DW 27.3-CR17`, which was split on 2026-07-29 and now covers only Story 27.3's C2 arm. `CR29`: an executed lane that loads `secretstore` **and** `access-telemetry-secrets` at their production `secretstores.hashicorp.vault` type against a reachable OpenBao and resolves their consumers' secretKeyRefs — cite the context, namespace, pod, command, UTC timestamp, and resolved key name, never a value. `CR30`: one lane that loads a vault-typed component and resolves a secretKeyRef through it, plus the assertion tying AC8's static lane to AC6's runtime lane. Or a re-disposition of either recorded in `deferred-work.md` with owner, consequence, and reopen trigger. The disposable `kind` lane is **out of scope** — already repaired by Story 27.3 under the Administrator's 2026-07-28 decision | pending | not complete | — |

## Dev Notes

### Developer Context

**Read this before planning: the component is already on `hashicorp.vault`.** Commit `4d2e4e2f`
(`feat(openbao): Implement OpenBao-first secret management in Hexalith`) migrated
`deploy/kubernetes/base/dapr/secretstore.yaml` to `secretstores.hashicorp.vault` with `scopes:
[eventstore, memories]`, and the deployed cluster has been running it for nine days. Story 31.1's Dev
Notes say so explicitly and instruct that this story claim the proof, not the migration.

So AC1's first clause is **structurally satisfied and already test-pinned**. This story's real deliverable
is the four things Epic 31 lists as Implementation evidence and that nothing in the repository proves today:

1. A live scoped read demonstrating both scopes actually resolve — currently blocked for `eventstore` (D1).
2. A per-Secret justification inventory. Fifteen Kubernetes Secrets exist in namespace `hexalith-memories`; **none** is currently classified against NFR9's two admitted classes.
3. A structural no-SDK proof. **No such test exists.** Epic 29 Story 29.2 was to deliver it and is still `backlog`; do not wait for it and do not claim its scope.
4. A negative proof that a resolved secret value cannot surface in logs, telemetry, CLI output, or a snapshot.

The temptation this story must resist is declaring victory on the component file. The component file is the
premise, not the outcome.

### Measured Runtime State At Creation

Measured 2026-07-28 by read-only probe against context `jpiquot@local`, namespace `hexalith-memories`.
**Re-derive all of it in Task 1.** No Secret contents were read, printed, or stored.

**Deployed runtime.** `secretstore` and six sibling Dapr components are deployed and 9 days old. Pods
`memories-*` and `memories-mcp-*` are `2/2 Running`, so `daprd` loaded the `hashicorp.vault` component
successfully — the store works in the real cluster. `openbao-runtime-bootstrap` **exists** here; its absence
in `DW 27.3-CR17` is specific to the disposable `kind` CI lane.

**Dapr app-ids that actually run:** `memories`, `memories-mcp`. Declared in manifests but not running:
`memories-access-telemetry`, `memories-access-telemetry-clock` (scaled to zero by the production overlay).
**No workload carries app-id `eventstore`** — only a ServiceAccount and an RBAC Role of that name exist.

**Secret-resolution topology.** `deploy/kubernetes/base/dapr/config.yaml` (`memories-config`) declares
`defaultAccess: deny` per store with explicit allow-lists:

| Store | Allowed secret names |
| :---- | :------------------- |
| `secretstore` | `redis-secret`, `google-embedding-api-key`, `memories-embedding-client-secret`, `llm-secret` |
| `access-telemetry-secrets` | `access-telemetry-marker-key` |

These names are **OpenBao keys** under prefix `hexalith/memories/runtime`, resolved through the Dapr Secrets
API. That four of them are *also* Kubernetes Secret names is the ambiguity this story's inventory must settle.

**Kubernetes Secrets present, and what references them.** This is the raw material for Task 3; the
`Likely class` column is a starting hypothesis, **not** a finding — re-derive each one.

| Secret | Referenced by | Likely class |
| :----- | :------------ | :----------- |
| `openbao-runtime-bootstrap` | `dapr/secretstore.yaml` `caPem` + `vaultToken`; RBAC `memories`, `eventstore` | `openbao-bootstrap` — the AC1 exemplar |
| `openbao-access-telemetry-bootstrap` | `dapr/access-telemetry-secrets.yaml`; RBAC access-telemetry roles | `story-27.3-retained` |
| `app-api-token` | pod env in server, mcp, access-telemetry deployments | `direct-pod-input` (Dapr platform env contract, project-context D30) |
| `dapr-api-token` | pod env in the same three deployments | `direct-pod-input` (same sanctioned exception) |
| `redis-secret` | `dapr/statestore.yaml`, `dapr/pubsub.yaml`, `dapr/access-telemetry-config-store.yaml` via `secretKeyRef`; pod env in server, redis, falkordb | **ambiguous** — both a Kubernetes-resolved component credential *and* a name in `secretstore`'s allow-list |
| `llm-secret` | `dapr/conversation-openai.yaml` via `secretKeyRef` | **ambiguous** — a component `secretKeyRef` resolves from the Kubernetes store, yet the name is also in `secretstore`'s allow-list |
| `google-embedding-api-key` | `memories-config` allow-list and RBAC only — **no component, env, or volume reference** | likely `residue` |
| `memories-embedding-client-secret` | `memories-config` allow-list and RBAC only — **no component, env, or volume reference** | likely `residue` |
| `access-telemetry-marker-key` | `access-telemetry-lifecycle-config.yaml` allow-list; pod env name reference | `story-27.3-retained` |
| `access-telemetry-clock-key` | pod env in access-telemetry deployments | `story-27.3-retained` |
| `access-telemetry-clock-sources` | pod env, three keys | `story-27.3-retained` |
| `access-telemetry-postgresql` | `dapr/access-telemetry-store.yaml` `secretKeyRef` | `story-27.3-retained` (`PG-ONPREM-1`) |
| `access-telemetry-postgresql-bootstrap` | volume in `access-telemetry-postgresql.yaml` | `story-27.3-retained` |
| `access-telemetry-postgresql-tls` | volumes in postgresql and access-telemetry deployments | `story-27.3-retained` |
| `registry-credentials` | `imagePullSecrets` on all five ServiceAccounts | `direct-pod-input` (kubelet pull, outside the Dapr boundary entirely) |

Namespace `openbao` additionally holds `openbao-seal`, `openbao-server-tls`, `hexalith-keys-pki`, and
`openbao-operator-credentials`. Those are Story 31.1 platform scope (`platform-31.1`); document the boundary,
do not re-litigate them.

**RBAC residue.** `memories-dapr-secret-reader` grants `get` on nine Secret names, four of which
(`redis-secret`, `google-embedding-api-key`, `memories-embedding-client-secret`, `llm-secret`) are now routed
through the OpenBao-backed `secretstore`. `eventstore-dapr-secret-reader` grants four names to a
ServiceAccount no pod uses. `ProductionDeploymentArtifactsTests.ProductionOverlay_SecretRoleIsResourceNameBound`
pins the resource-name binding, so narrowing these grants will touch that test — narrow it deliberately, do
not delete assertions to get green.

Read-only probes used (safe to repeat; none reads a Secret's contents):

```bash
kubectl config current-context
kubectl -n hexalith-memories get secret --no-headers -o custom-columns=NAME:.metadata.name,TYPE:.type
kubectl -n hexalith-memories get components.dapr.io
kubectl -n hexalith-memories get pods -o wide
kubectl -n hexalith-memories get pods -o jsonpath='{range .items[*]}{.metadata.name}{"  app-id="}{.metadata.annotations.dapr\.io/app-id}{"\n"}{end}'
grep -rn "secretKeyRef\|secretName\|imagePullSecrets" deploy/kubernetes deploy/openbao
```

### Open Decisions

Both are genuine scope questions, not implementation details. Route them rather than resolving them silently;
`story-scope-guard.md` sends ambiguous scope to `decision_needed`.

**All three decisions were settled on 2026-08-01 by the Administrator, before development, across the two
Sprint Change Proposals of that date.** Each analysis below is kept for provenance and carries its dated
resolution; a routed decision that was answered is evidence, not clutter. **No unrouted decision remains
outstanding for this story.**

**D1 - The `eventstore` scope cannot be proven by a live read.** AC1 requires `hashicorp.vault` with the
`eventstore` **and** `memories` scopes, and Epic 31's Implementation evidence requires "both scopes proven by
a live scoped read". No workload carries app-id `eventstore`; the EventStore is consumed as the
`references/Hexalith.EventStore` submodule and `src/Hexalith.Memories.EventStore` project, not as a separately
deployed Dapr app. The scope, its ServiceAccount, and its RBAC Role are declared for a workload that does not
run. Candidate resolutions, for the Administrator to choose:
(a) prove `eventstore` structurally — declared scope plus a demonstrated denial from a non-scoped app-id — and
record explicitly that no `eventstore` workload is deployed, with an owner and a reopen trigger;
(b) deploy or temporarily run an `eventstore`-app-id sidecar to obtain a genuine read;
(c) amend AC1 by approved sprint change if the `eventstore` scope is reserved rather than active.
Do **not** silently pick (a) and describe it as a live read — that is precisely the "internal-only proof where
observable evidence is required" that the scope guard rates `high`.

> **RESOLVED 2026-08-01 — (c) + (a): the `eventstore` scope is reserved.** Decision owner: Administrator
> (`jpiquot`), recorded by approved Sprint Change Proposal 2026-08-01 (Story 31.2 reserved `eventstore` scope
> and deferred-work retarget). Re-derivation at HEAD `1d9e9c89` made the finding stronger than stated above:
> not only does no pod carry app-id `eventstore`, **no manifest in `deploy/kubernetes/` declares one** —
> `grep -rn 'dapr.io/app-id' deploy/kubernetes/` returns exactly `memories`, `memories-mcp`,
> `memories-access-telemetry` and `memories-access-telemetry-clock`. AC1 is amended here and in `epics.md` to
> require the `memories` scope by live scoped read and the `eventstore` scope structurally — declared presence
> plus a demonstrated denial from a non-scoped app-id. Candidate (b) was **rejected**: standing up a throwaway
> `eventstore` sidecar would fabricate a workload the topology does not have, mutate the deployed cluster this
> story puts out of scope, and prove nothing about production. Owner: Memories Maintainer. Reopen trigger: an
> `eventstore`-app-id workload is deployed, at which point the live read becomes both possible and required.
> The guard above still binds: the structural proof must never be described as a live read.

**D2 - How `DW 27.3-CR17` is discharged.** The disposable `kind` lane has no OpenBao and no
`openbao-runtime-bootstrap` Secret, so `daprd` fails closed and aggregate health is unreachable by
construction. Candidate resolutions: stand up OpenBao in the lane; seed a stub bootstrap Secret against a
minimal vault-compatible endpoint; patch the lane's overlay to exclude the `secretstore` component and record
the reduced coverage; or re-disposition CR17 with owner, consequence, and reopen trigger. Each has a different
blast radius on `tools/verify-production-deployment.ps1`, which two other lanes pin. Excluding the component
weakens exactly the fail-closed behavior Task 7 asserts — if that path is chosen, say so plainly.

> **RESOLVED 2026-08-01 — the question above is moot; this story targets `DW 27.3-CR29` and `DW 27.3-CR30`.**
> Decision owner: Administrator (`jpiquot`), recorded by approved Sprint Change Proposal 2026-08-01 (Story
> 31.2 reserved `eventstore` scope and deferred-work retarget). Two things changed after this story was
> written on 2026-07-28. **First**, the `kind`-lane repair was already chosen and executed: on 2026-07-28 the
> Administrator decided on verification-scoped `secretstores.kubernetes` substitution with a mandatory
> `secret-store-substitution.json` disclosure that `tools/validate-production-deployment-evidence.ps1` fails
> closed on, mutation-proven both directions, in commits `564d5d56` and `64434e57` — inside Story 27.3, not
> here. **Second**, `DW 27.3-CR17` was **split on 2026-07-29** by Story 27.3's chunk-2 code review and now
> covers only the Story 27.3 / checkpoint C2 arm; the Story 31.2 arm moved to `DW 27.3-CR29`, and `DW
> 27.3-CR30` was minted alongside it. Both are `open`, both name Story 31.2 as owner, and both fire before
> this story is set `done`. `CR29` is dischargeable on `jpiquot@local`, which has a reachable OpenBao and
> vault-typed components loaded, so it folds into Task 4 rather than becoming CI work — but a loaded component
> is not a resolved secret, so the discharge needs Task 4's executed secretKeyRef resolution.
> `tools/verify-production-deployment.ps1` leaves this story's scope and ownership entirely.

**D3 - `epics.md` declares one owned path; this story needs several.** Epic 31's **Owned paths** for Story
31.2 lists only `deploy/kubernetes/base/dapr/secretstore.yaml`, yet its own Implementation-evidence clause
requires an inventory, a structural proof, and a negative proof — none of which can live in that file. Story
31.1 hit this exact contradiction and code review raised it as a finding. Widen the `epics.md` Owned-paths
list under an approved sprint change, or accept the story file as sole authority and say so explicitly.

> **RESOLVED 2026-08-01 — `epics.md` Owned paths widened.** Decision owner: Administrator (`jpiquot`),
> recorded by approved Sprint Change Proposal 2026-08-01 (Story 31.1 checkpoint split and Epic 31 activation
> gate), §4.3. The single-path list now enumerates the manifest, the RBAC file, the jointly-owned operations
> document, the two test files, this story's evidence artifact, and `deferred-work.md`. The story file is not
> sole authority; `epics.md` and this file agree.

### Epic AC Verification

Verified 2026-08-01 against HEAD `1d9e9c89`, by approved Sprint Change Proposal 2026-08-01 (Story 31.1
checkpoint split and Epic 31 activation gate). This story was registered 2026-07-28 without this section,
which `_bmad/custom/epic-ac-verification.md` requires and fail-closes on; adding it is a compliance repair,
not a reconstruction of history. Every row was re-derived against current source, not inherited.

| Epic claim | Class | Command / evidence | Observed | Verdict |
| :--------- | :---- | :----------------- | :------- | :------ |
| "`deploy/kubernetes/base/dapr/secretstore.yaml` uses `hashicorp.vault` with the `eventstore` and `memories` scopes" | Existence | `grep -n -e 'type:' -e 'scopes:' -A2 deploy/kubernetes/base/dapr/secretstore.yaml` | **Re-derived 2026-08-01 at HEAD `1d9e9c89`:** line 6 is `type: secretstores.hashicorp.vault`, lines 31-33 are `scopes:` / `- eventstore` / `- memories`. Migrated by commit `4d2e4e2f` and pinned by `ProductionDeploymentArtifactsTests.ProductionOverlay_RendersExactSecurityPersistenceAndResourceContracts` | `confirmed` — the tracked file. **Task 2 still re-derives the *deployed* component against the cluster; the tracked file matching is not evidence that the running component does** |
| "no product project contains an OpenBao SDK, HTTP client, endpoint, or provider credential" | Absence | The Task 5 structural guard, once authored | **No such guard exists today.** Epic 29 Story 29.2 was to deliver it and is `backlog` | `confirmed` as a gap this story closes — not as an already-held property |
| "Story 31.1 is `done`" (inherited activation gate, `epics.md:5300`) | Behavioral | `grep -n '31-1-openbao' _bmad-output/implementation-artifacts/sprint-status.yaml` | `in-progress`, and unreachable while C4b/C5b/C7 carry an un-obtained independent countersignature | **`corrected`** — planning artifact corrected in the same change; the gate now binds on Story 31.1's documentation checkpoints C1/C2/C3/C4a/C5a/C6 |
| "Epic 31 Owned paths for Story 31.2 is `deploy/kubernetes/base/dapr/secretstore.yaml`" | Location | `sed -n '5321p' _bmad-output/planning-artifacts/epics.md` | Exactly one path, contradicting this story's own Implementation-evidence clause | **`corrected`** — `epics.md` Owned paths widened in the same change; discharges Open Decision **D3** |
| "no workload in namespace `hexalith-memories` carries app-id `eventstore`" (basis of Open Decision D1) | Existence | `kubectl -n hexalith-memories get pods -o jsonpath='{range .items[*]}{.metadata.annotations.dapr\.io/app-id}{"\n"}{end}'` | Measured read-only 2026-07-28: only `memories` and `memories-mcp` run | `confirmed` at creation — **Task 1 must re-derive before Task 4 relies on it** |
| "`Hexalith.Memories.Server.Tests` discovers 2,200 xUnit methods, with no external same-lane delta to separate" | Quantitative | `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` after a clean Release build | **2,203** at HEAD `1d9e9c89` — the baseline is stale and an external delta **does** exist | **`corrected`** — see `### Testing Baseline and Planned Delta`; +3 owned by Story 31.1 |
| "`deploy/dapr/components/secretstore.yaml` still declares `type: secretstores.kubernetes` under the same component name and scopes" | Existence | `grep -n -e 'type:' -e 'scopes:' -A2 deploy/dapr/components/secretstore.yaml` | **Re-derived 2026-08-01 at HEAD `1d9e9c89`:** line 6 is `type: secretstores.kubernetes`, lines 9-11 are `scopes:` / `- eventstore` / `- memories` — same component name and same two scopes as the migrated Kubernetes manifest. Out-of-scope collision hazard owned by Story 29.2 (`backlog`) | `confirmed` — **Task 7 must still scope its assertion to `deploy/kubernetes/**` deliberately; a repo-wide assertion fails on this file immediately** |
| "no workload carries app-id `eventstore`" narrowed to "no manifest declares app-id `eventstore`" | Absence | `grep -rn 'dapr.io/app-id' deploy/kubernetes/` plus `kubectl -n hexalith-memories get pods -o jsonpath=…` | Four declarations — `memories`, `memories-mcp`, `memories-access-telemetry`, `memories-access-telemetry-clock` — **none `eventstore`**; only `memories` and `memories-mcp` pods carry an app-id | **`corrected`** — stronger than the creation claim. AC1 amended 2026-08-01 to treat `eventstore` as a **reserved** scope; see Open Decision D1 |
| "`DW 27.3-CR17` … names Stories 31.1 and 31.2 as owner, and its reopen trigger fires 'before Story 31.1 or Story 31.2 is set `done`'" (Task 8 at creation) | Behavioral | Read the `Re-open trigger` field of `27.3-CR17` in `deferred-work.md` | **Split 2026-07-29**; `CR17` now covers only the Story 27.3 / C2 arm. This story's arm is `DW 27.3-CR29`, joined by `DW 27.3-CR30` | **`corrected`** — Task 8 and checkpoint C5 retargeted 2026-08-01; see Open Decision D2 |

Three rows are deliberately left as "confirmed at creation, re-derive at implementation". They are cluster- or
source-state claims that can drift between creation and development, and this story's own tasks already
require re-measurement. Recording them as permanently settled would be the drift this policy exists to catch.

### Scope Boundary

**In scope:** `deploy/kubernetes/base/dapr/secretstore.yaml`, the per-Secret justification inventory, the
structural no-SDK proof, the runtime secret-resolution negative proof, the fail-closed cutover proof, the
supporting tests, this story's evidence artifact, and the `DW 27.3-CR29` and `DW 27.3-CR30` dispositions
(**retargeted 2026-08-01**; `DW 27.3-CR17` was split on 2026-07-29 and its remaining arm belongs to Story 27.3).

**Out of scope — Story 31.1:** `deploy/openbao/**` and the measured-platform sections of
`docs/operations/openbao.md`. That file becomes jointly owned; append the inventory, do not rewrite 31.1's
content, and do not re-assert its accepted-limitations table, which its own guard fixes at exactly two rows.

**Out of scope — Epic 29:** `src/Hexalith.Memories.AppHost/**`, `src/Hexalith.Memories.Aspire/**`,
`deploy/dapr/**`, and the AppHost OpenBao tests. Story 29.1 is `done`; Story 29.2 is `backlog` and owns
provider-neutral Aspire composition. Do not implement 29.2 here, and do not close its ACs from this evidence.

**Specifically out of scope, and it will look like a bug:** `deploy/dapr/components/secretstore.yaml` still
declares `type: secretstores.kubernetes` with the same component name and the same `eventstore`/`memories`
scopes as the migrated Kubernetes manifest. That standalone template is Story 29.2's explicit AC ("standalone
Dapr templates … follow the OpenBao-first rule"). Name it in the inventory with Story 29.2 as owner and a
reopen trigger; do not migrate it here, and do not let a repo-wide assertion in Task 7 collide with it.

**Out of scope — Story 27.3:** the access-telemetry-specific secret components and the `PG-ONPREM-1` secret
backing. Classify them in the inventory as retained; do not migrate them.

**Out of scope — the deployed platform:** no `helm upgrade`, no change to the running OpenBao release, no
rotation of any bootstrap credential. Live cluster access is read-only except for the Task 4 scoped-read
proof, which reads one allow-listed secret through the Dapr API and stores no value.

### Slice Proof

**Thin vertical slice:** prove that the already-migrated runtime `secretstore` resolves through OpenBao under
both declared scopes and fails closed otherwise; classify every remaining Kubernetes Secret against NFR9's two
admitted classes and disposition the residue; and bind the no-SDK and no-leak invariants with executable
guards.

This is the minimum coherent slice because a migration whose scopes are never exercised is a configuration
claim, not a boundary; and an inventory that omits residue lets a Kubernetes-Secret path survive under an
OpenBao-first label. It stops short of Epic 29's provider-neutral composition and of Story 27.3's
access-telemetry secrets, both of which are independently owned outcomes.

**Anti-bundling check:** the deferred-work discharge (Task 8) is an independently demonstrable outcome and is
therefore carried as a `done` gate with its own checkpoint row, **not** folded into AC1 or AC2. **Restated
2026-08-01** by approved Sprint Change Proposal 2026-08-01: this paragraph previously described Task 8 as "the
`DW 27.3-CR17` CI repair". That framing is void. The CI-lane repair was decided by the Administrator on
2026-07-28 and executed inside Story 27.3, and `DW 27.3-CR17` was split on 2026-07-29 so that its remaining
arm covers only Story 27.3's checkpoint C2. Task 8 now discharges `DW 27.3-CR29` and `DW 27.3-CR30` — proving
the vault-typed path on an executed lane — which is still an outcome independent of AC1 and AC2. Standing up
OpenBao in the disposable CI lane remains a separate story, not a subtask, and is not required by either entry.

### Security and Documentation Guardrails

- **Never expose secrets** (project-context critical rule, NFR9). No token, CA private material, unseal or recovery key, or resolved secret value in the inventory, the evidence artifact, a test fixture, a snapshot, or a command transcript. `kubectl get secret` for names and types only.
- The Task 4 positive read is the one place a secret value is in flight. Record the store name, key name, HTTP status, and nothing else. Never pipe a Dapr secrets response body to a file or a log.
- Architecture **D31** is the invariant this story proves: product services reach secrets only through the Dapr Secrets API, with cross-prefix reads failing closed. This story adds no code path to OpenBao.
- The OpenBao prefixes are shared **application** scopes, not tenant partitions. Do not describe them as per-tenant isolation.
- Dapr documents OpenBao through `secretstores.hashicorp.vault`; there is no separate OpenBao component type.

### Remediation Runtime Checklist Applicability

Classified from the actual change surfaces, per `_bmad/custom/remediation-runtime-checklist.md`.

| Category | Applicability | Coverage |
| :------- | :------------ | :------- |
| 1 - Dapr workflow activity registration | **Not applicable** — no workflow, child workflow, or activity registration is touched. This story changes a Dapr *component* and its proofs | — |
| 2 - Observed child workflows | **Not applicable** — no orchestration is started or awaited | — |
| 3 - Owner-checked cleanup and dedup | **Not applicable** — no cleanup, deletion, compensation, or dedup of shared or tenant-scoped state. Dropping a residual RBAC grant is a least-privilege narrowing, not a state mutation | — |
| 4 - Rollback and cutover safety | **Applicable** — the runtime secret-resolution boundary is cut over from Kubernetes Secrets to `hashicorp.vault` | **Task 7**: adversarial proof that an unauthenticated or unreachable store fails closed and observably, and never silently falls back to a Kubernetes-Secret-backed store or an empty successful read. Guards the real defect recorded in `DW 27.3-CR17` |
| 5 - File List reconciliation | Discharged by `story-phase-ledger.md` Cumulative File List Reconciliation (`matched N/N`); not duplicated here | — |

Re-derive this classification from the actual development diff before handoff rather than inheriting it. If
D2 resolves toward changing the deployed platform or the CI rollout path, re-derive before proceeding.

### Tenant Isolation Evidence Applicability

**Not applicable at creation — no tenant surface touched.** This story changes no tenant or case routing,
endpoint filter, auth claim, tenant status, index/key/graph selection, actor ID, storage or query selector,
MCP authorization or execution, evidence scope display, verifier marker, attribution, or tenant-scoped data
movement. The `scopes` on `secretstore.yaml` are Dapr **app-id** scopes and the OpenBao prefixes are shared
application scopes; neither is a tenant partition.

Confirm rather than assume: run `python3 tools/check-tenant-isolation-evidence.py --changed-files-file <changed>`
over the final changed-file set and record the output verbatim. A secrets-boundary story will be asked, so
state the result explicitly in the completion record instead of leaving it implied.

### Expected File Ownership

| Disposition | Path | Story 31.2 expectation |
| :---------- | :--- | :--------------------- |
| Verify / Update | `deploy/kubernetes/base/dapr/secretstore.yaml` | Already on `hashicorp.vault` with both scopes; change only if re-measurement shows divergence |
| Update | `docs/operations/openbao.md` | **Append** the per-Secret inventory. Story 31.1's measured-platform sections are frozen |
| Update | `deploy/kubernetes/base/service-accounts-rbac.yaml` | Only if Task 3 dispositions residual grants; collides with `ProductionOverlay_SecretRoleIsResourceNameBound` |
| New | `tests/Hexalith.Memories.Server.Tests/Deployment/RuntimeSecretStoreMigrationTests.cs` | Component contract, inventory binding, no-SDK structural scan, negative secret-safety proof, fail-closed cutover proof |
| Update | `tests/Hexalith.Memories.Server.Tests/Deployment/ProductionDeploymentArtifactsTests.cs` | Only if an RBAC or component change collides; no assertion deleted to get green |
| New | `_bmad-output/implementation-artifacts/tests/31-2-runtime-secret-store-evidence.md` | Measurement transcript, live scoped-read and denial results, mutation records |
| Update | `_bmad-output/implementation-artifacts/deferred-work.md` | `DW 27.3-CR29` and `DW 27.3-CR30` dispositions only (**retargeted 2026-08-01**; `DW 27.3-CR17` was split 2026-07-29 and its remaining arm is Story 27.3's) |
| Preserve | `tools/verify-production-deployment.ps1` | **Out of scope as of 2026-08-01.** The disposable-lane repair is already applied and owned by Story 27.3 (commits `564d5d56`, `64434e57`). Cross-pinned by `CiTestInventoryTests` and the `publish_containers` renderer path |
| Update | `_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md` | This story file |
| Update | `_bmad-output/implementation-artifacts/sprint-status.yaml` | Status transitions only |
| Preserve | `deploy/openbao/**` | Story 31.1 |
| Preserve | `src/Hexalith.Memories.AppHost/**`, `src/Hexalith.Memories.Aspire/**`, `deploy/dapr/**` | Epic 29 |
| Preserve | `deploy/kubernetes/base/dapr/access-telemetry-*.yaml` | Story 27.3 |

Treat this table as guidance, not permission to overwrite concurrent work. Re-read every path immediately
before editing and reconcile actual paths in the phase ledger.

### Testing Baseline and Planned Delta

Creation used a fresh Release build that completed with 0 warnings and 0 errors:

```text
DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
```

The runner-derived baseline uses the named unit **xUnit test method**:

```text
DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo
```

| Lane | Creation baseline (methods) | Planned Story 31.2 delta |
| :--- | --------------------------: | -----------------------: |
| `Hexalith.Memories.Server.Tests`, all xUnit methods | 2,200 | +6..10 |
| `Hexalith.Memories.Server.Tests.Deployment` namespace | 58 | +6..10 (all of the above) |
| `RuntimeSecretStoreMigrationTests` | 0 (class does not exist) | new class, +6..10 |
| `ProductionDeploymentArtifactsTests` | 9 | +0 expected; assertions may change if RBAC narrows |
| `OpenBaoPlatformDocumentationTests` | 10 | +0; regression only — it reads `docs/operations/openbao.md`, which this story appends to |
| `AppHostOpenBaoConfigurationTests` | 5 | +0; Epic 29 scope, read-only reference |
| `DeploymentConfigurationContractTests` | 7 | +0; pattern source |

Sorted method-set SHA-256 at creation:
`eab323be548a65055fe86d0a421909b238bdaa33975719b6c3fd50ee02b656ae`.

This hash is byte-identical to Story 31.1's post-code-review snapshot, so this baseline is continuous with
that story's ledger and carries **no external same-lane delta** to separate.

**Corrected 2026-08-01 by approved Sprint Change Proposal 2026-08-01 (Story 31.1 checkpoint split and Epic 31
activation gate): the paragraph immediately above is now stale, and an external same-lane delta of +3 does
exist.** Re-measured at HEAD `1d9e9c89` after a clean Release build (0 warnings, 0 errors) with the same
runner, discovery scope, filters and configuration as the creation baseline, so the subtraction is between
comparable discoveries:

| Lane | Recorded create baseline | Observed 2026-08-01 | External delta |
| :--- | -----------------------: | ------------------: | -------------: |
| `Hexalith.Memories.Server.Tests`, all xUnit methods | 2,200 | **2,203** | **+3** |
| `Deployment` namespace | 58 | **61** | **+3** |
| `OpenBaoPlatformDocumentationTests` | 10 | **13** | **+3** |
| `ProductionDeploymentArtifactsTests` | 9 | 9 | +0 |
| `RuntimeSecretStoreMigrationTests` | 0 | 0 | +0 |

Observed sorted method-set SHA-256:
`aacac6f53f49f2a7450345646e76fc7ffde2e896d716f8785ab08457f347f22a`.

The three methods are `DeployedProfileTable_PinsEveryMeasuredRowByKey`,
`OperationalSections_StayBoundToTheirRecordedRemediations`, and
`SecretShapeGuard_ExcusesALabelledDigestAndReportsAnUnlabelledPaste`. They are owned by **Story 31.1's
second-pass code review** and landed in commit `a4517654`, verified with
`git diff 115d30b5..HEAD -- tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs`.

The cause is worth recording so it is not repeated: commit `a4517654` bundled this story's `create-story`
output **and** Story 31.1's post-review test additions, while this story's `baseline_commit` is `115d30b5` —
that commit's parent. The baseline was therefore measured one commit before three methods that landed
alongside this very story file.

**Reconciliation any `dev-story` phase must carry forward:** create baseline **2,200** + Story 31.2 delta
**0** + external delta **+3** (owner: Story 31.1, commit `a4517654`) = observed **2,203**. Do not absorb the
+3 into this story's delta, and re-measure before development rather than trusting either number here.

Do **not** use a built-assembly SHA-256 as count evidence: Story 31.1 retired it after two Release builds of
unchanged sources produced different assembly hashes with a byte-identical method set. Use the sorted
method-set hash.

Planned ranges are estimates, never evidence. At implementation time rebuild first, capture pre/post runner
inventories, state any external same-lane delta separately, and record only observed comparable changes.

`dotnet test` fails in this sandbox with a `SocketException (13)`; use `dotnet exec` against the xUnit v3
assembly with `DiffEngine_Disabled=true`, as every command above does.

### Historical Context Classification

Required by `_bmad/custom/story-scope-guard.md` because prior stories influence this draft. Numeric adjacency
is not a relevance signal; every row below was re-verified against current source at creation.

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Current deployed runtime, measured read-only 2026-07-28 | current-narrow-pattern | The authoritative subject; all claims derive from re-measurement |
| Current `deploy/kubernetes/base/dapr/**` and `service-accounts-rbac.yaml` | current-narrow-pattern | The measured secret-resolution topology, re-verified against the live cluster |
| Commit `4d2e4e2f` | historical-reference-only | Provenance of the `hashicorp.vault` component and both scopes. It performed the migration; this story proves it |
| Story 31.1 **whole-story shape** | anti-template | Its 7-task drift-measurement structure, 7-checkpoint table, 38-finding review, ten-mutation evidence model, and 12-path File List must not shape this story. Epic 31 mandates **four** evidence rows here, not seven |
| Story 31.1 security guardrails (never read Secret contents, prefixes are application scopes, D31 invariance) | current-narrow-pattern | Re-verified constraints that still bind, carried forward deliberately |
| Story 31.1 `OpenBaoPlatformDocumentationTests` negative-guard mechanism | current-narrow-pattern | The AC3 pattern set only — PEM markers, the six token prefixes, `bao operator init` labels, base64 density including `/` and `=`. Its subject matter and scope are not reused; this story scans its own records |
| `MarkdownContractDocument` / `ContractDocumentGuard` / `DeploymentConfigurationContractTests` | current-narrow-pattern | The structure-aware table/section assertion mechanism, per the project-context testing rule on contract-document guards |
| `SubmoduleGuardTests` | current-narrow-pattern | Its repo-root location and source/`.csproj` scanning mechanism for the Task 5 structural guard; not its subject |
| Story 26.1, origin of `ProductionDeploymentArtifactsTests` | current-narrow-pattern (whole-story shape is `anti-template`) | Only the assertion mechanism, re-verified by a passing regression run. Its whole-story shape is classified `anti-template` by Stories 27.1, 27.2, 27.3, 29.1 and 31.1 |
| Pre-split Story 31.1 (platform + runtime migration bundled) | anti-template | Superseded by the approved 2026-07-27 split. Do not re-absorb platform hardening into this story |
| Story 27.3 whole checkpoint shape | anti-template | Its 25-row C1 gate table and Checkpoint Execution Contract must not be copied. Only the four column semantics Epic 31 mandates are reused |
| Story 29.1 whole-story shape | anti-template | Its AppHost slice, live-Aspire evidence model, and 27-path File List must not shape this story |
| Story 29.2 (`backlog`, provider-neutral Aspire composition) | historical-reference-only | Overlapping no-SDK language. Its scope is Epic 29's; do not implement or close it here |
| `DW 27.3-CR6`, `CR16` | historical-reference-only | Split provenance only |
| `DW 27.3-CR17` | historical-reference-only | **Reclassified 2026-08-01.** Split on 2026-07-29; its remaining arm is Story 27.3's checkpoint C2. Cite it only as provenance for the observed fail-closed `daprd` exit and for the split itself — never as this story's gate |
| `DW 27.3-CR29`, `DW 27.3-CR30` | current-narrow-pattern | **Joined 2026-08-01.** The two open entries that name Story 31.2 as owner and fire before it is set `done`. Carried as checkpoint C5 and Task 8, never as an AC |

### Git Intelligence

- Creation baseline is `115d30b5` (`Update submodule references and enhance OpenBao platform documentation tests`), with a **clean** worktree — unlike Story 31.1, which was created against a dirty tree and carried nine named exclusions. Any dirty path at implementation time joined after creation; attribute it before absorbing it.
- `4d2e4e2f` and `7217ef89` are the only commits that have ever touched `deploy/kubernetes/base/dapr/secretstore.yaml`. The tracked file has been stable since; the drift risk here is the *cluster*, as Story 31.1 demonstrated across nine unnoticed Helm revisions.
- The three most recent commits (`115d30b5`, `1868c8f9`, `327d1a9d`) are Story 31.1 and Story 27.3 work. None touches this story's owned paths.
- Recent convention worth following: `1868c8f9` and `115d30b5` land a documentation guard alongside the document it binds, in the same commit. Do the same — an inventory with no executable binding drifts within the week.
- Use Conventional Commits. This story is `fix:` or `test:`, **not** `feat:` — it proves and documents an existing capability rather than adding one, and `feat` triggers a minor release.

### Latest Technical Information

- **Dapr secret scoping is two independent mechanisms, and AC1 touches both.** Component `scopes:` restricts which *app-ids* receive the component at all; the Configuration `secrets.scopes` allow-list restricts which *secret names* a scoped app may read. `memories-config` sets `defaultAccess: deny` on both stores, so an unlisted name fails even for a scoped app. Task 4 must exercise both layers or the "fail closed" claim covers only half the surface.
- **`secretKeyRef` inside a Dapr component manifest resolves from the *Kubernetes* secret store**, not from the component being defined. That is why `secretstore.yaml` bootstrapping from `openbao-runtime-bootstrap` is the sanctioned exception D31 names — and why `conversation-openai.yaml`'s `llm-secret` reference is a genuine open question for the inventory rather than a formality.
- **Dapr 1.18 enables HotReload by default; `memories-config` disables it** because actor state stores cannot reload in place. A secret-store component edit therefore requires restarting the owning workload — a component change alone proves nothing about the running system.
- **`hashicorp.vault` metadata pins currently in force:** `skipVerify: "false"`, `tlsServerName: hexalith-keys.openbao.svc.cluster.local`, `enginePath: secret`, `vaultKVUsePrefix: "true"`, `vaultValueType: map`. `vaultValueType: map` means a single OpenBao key returns a map of fields — relevant to how the Task 4 read is shaped and to what a leak would look like.
- OpenBao `2.6.0` is the pinned and running version. This story changes no pin.

### Project Structure Notes

- Work in the root repository and use `Hexalith.Memories.slnx`; do not create a legacy `.sln`.
- `.editorconfig` and `Directory.Build.props` enforce nullable, analyzers, warnings-as-errors, implicit usings, and one type per file. New C# files require the ITANEO copyright header.
- Test naming is descriptive PascalCase; assertions use Shouldly, never raw `Assert.*`. A global `Xunit` using already exists. Place deployment tests under `tests/Hexalith.Memories.Server.Tests/Deployment/`.
- `.gitattributes` is authoritative: `*.md` and `*.cs` materialize CRLF, `*.yaml` stays LF. Normalize Markdown with the idempotent two-step `sed -i -e 's/\r$//' -e 's/$/\r/'`; the bare append-CR form corrupts already-CRLF files to `\r\r\n`.
- If a build reports "Build FAILED, 0 Errors", rerun with `-v:m` — the console logger's default filtering hides real compile errors.
- Do not initialize or update nested submodules, update dependencies, stage, commit, push, or clean the worktree unless separately requested. The `references/` gitlinks are user-owned and drift on their own; revert with `git submodule update -- <paths>`.

### References

- [Epic 31 and Story 31.2](../planning-artifacts/epics.md#story-312-runtime-dapr-secret-store-migration-to-hashicorpvault)
- [Sprint Change Proposal 2026-07-27 — Epic 31 split](../planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md#44-cr16--epic-31-split-1-story--2)
- [Architecture D31 — OpenBao-first DAPR secret provider](../planning-artifacts/architecture.md#d31--openbao-first-dapr-secret-provider)
- [Architecture D30 — no infrastructure dependency in product code](../planning-artifacts/architecture.md#decision-registry)
- [PRD NFR9](../planning-artifacts/prd.md#non-functional-requirements)
- [Canonical project context](../project-context.md)
- [Deferred work: DW 27.3-CR29 and DW 27.3-CR30](./deferred-work.md)
- [Sprint Change Proposal 2026-08-01 — Story 31.1 checkpoint split and Epic 31 activation gate](../planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md)
- [Sprint Change Proposal 2026-08-01 — Story 31.2 reserved `eventstore` scope and deferred-work retarget](../planning-artifacts/sprint-change-proposal-2026-08-01-story-31-2-reserved-eventstore-scope-and-deferred-work-retarget.md)
- [Story 31.1 — platform hardening (gate; do not reuse its shape)](./31-1-openbao-platform-hardening-and-documentation.md)
- [Story 29.1 — Aspire-local OpenBao topology (done, out of scope)](./29-1-openbao-backed-apphost-secret-topology.md)
- [Operations document to append the inventory to](../../docs/operations/openbao.md)
- [Dapr HashiCorp Vault secret store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/hashicorp-vault/)
- [Dapr secret scoping](https://docs.dapr.io/operations/configuration/secret-scope/)
- [Dapr component scopes](https://docs.dapr.io/operations/configuration/component-scopes/)
- [Dapr Secrets API](https://docs.dapr.io/reference/api/secrets_api/)
- [OpenBao KV secrets engine](https://openbao.org/docs/secrets/kv/)

## Dev Agent Record

### Agent Model Used

Claude Opus 5 (`claude-opus-5`)

### Debug Log References

- 2026-07-28 (create-story): Loaded Epic 31, the 2026-07-27 split proposal, architecture D30/D31, PRD NFR9, canonical project context, the story-scope guard, the story phase ledger, the remediation runtime checklist, story-creation lessons, Story 31.1 in full, `DW 27.3-CR17`, and the current tracked Dapr components, RBAC, deployment manifests, and deployment tests.
- 2026-07-28 (create-story): Measured namespace `hexalith-memories` read-only against context `jpiquot@local`. No Secret contents were read, printed, or stored. Confirmed `openbao-runtime-bootstrap` exists live and the `secretstore` component is loaded — so `DW 27.3-CR17`'s failure is specific to the disposable `kind` lane, not the real cluster.
- 2026-07-28 (create-story): Found that no workload carries app-id `eventstore`, making AC1's "both scopes proven by a live scoped read" unsatisfiable as written. Routed as decision D1 rather than resolved inside the story.
- 2026-07-28 (create-story): Found that `memories-config` routes four secret names through the OpenBao-backed `secretstore` while `memories-dapr-secret-reader` still grants Kubernetes `get` on the same names, and that `google-embedding-api-key` and `memories-embedding-client-secret` have no component, env, or volume reference at all. Recorded as the inventory's residue hypothesis.
- 2026-07-28 (create-story): Found `deploy/dapr/components/secretstore.yaml` still on `secretstores.kubernetes` under the same component name and scopes as the migrated Kubernetes manifest. Confirmed it is Story 29.2's explicit AC scope and recorded it as an out-of-scope collision hazard for Task 7's assertion rather than pulling it into this story.
- 2026-07-28 (create-story): Captured the fresh Release build and runner-derived discovery baseline; the sorted method-set hash reproduces Story 31.1's post-review snapshot exactly.

### Completion Notes List

- 2026-07-28: Created Story 31.2 as the runtime secret-store **proof** slice. The component migration itself landed in commit `4d2e4e2f`; this story proves both scopes, justifies every remaining Kubernetes Secret, and binds the no-SDK and no-leak invariants.
- 2026-07-28: Recorded the blocking activation gate (Story 31.1 `done`), which was open at creation.
- 2026-07-28: Routed three scope questions as explicit decisions (D1 `eventstore` scope unprovable live, D2 `DW 27.3-CR17` repair path, D3 `epics.md` Owned-paths narrower than the story's own evidence clause) rather than resolving them silently.
- 2026-07-28: Ultimate context engine analysis completed — comprehensive developer guide created.

### File List

- `_bmad-output/implementation-artifacts/31-2-runtime-dapr-secret-store-migration.md` — **new.** This story file.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — **partial ownership.** Story 31.2 owns only the `31-2-runtime-dapr-secret-store-migration` status row and the `last_updated` field.
- `_bmad-output/planning-artifacts/epics.md` — **joined at correct-course 2026-08-01. Partial ownership.** Story 31.2 owns only its own **Activation gate** and **Owned paths** hunks under `### Story 31.2`. Story 31.1 owns its own hunks in the same file and is unchanged by this correction.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md` — **joined at correct-course 2026-08-01.** New. The approved proposal that rebinds this story's activation gate, widens its `epics.md` Owned paths, adds its `Epic AC Verification` section, and corrects its testing baseline. Jointly owned with Story 31.1, which credits it in its own File List.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-28 | create-story | Created context-ready Story 31.2 and moved `31-2-runtime-dapr-secret-store-migration` from `backlog` to `ready-for-dev`. `epic-31` was already `in-progress` and is unchanged; `31-1-openbao-platform-hardening-and-documentation` is left `in-progress` and is this story's blocking activation gate. Recorded the measured runtime secret-resolution topology, the fifteen-Secret inventory input, the RBAC residue hypothesis, and three routed decisions (D1 `eventstore` scope unprovable by live read, D2 `DW 27.3-CR17` repair path, D3 narrow `epics.md` Owned-paths). No implementation, manifest, documentation, dependency, deployed-platform, or `deferred-work.md` change occurred. | Actual phase delta **+0**, cumulative **+0**. Fresh Release build passed with 0 warnings / 0 errors: `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`. Runner-derived baseline, named unit **xUnit test method**, exact discovery command `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo`: `Hexalith.Memories.Server.Tests` **2,200** methods, of which `Deployment` namespace **58**, `OpenBaoPlatformDocumentationTests` **10**, `ProductionDeploymentArtifactsTests` **9**, `OperationalRunbookSetTests` **9**, `DeploymentConfigurationContractTests` **7**, `AppHostOpenBaoConfigurationTests` **5**, and `RuntimeSecretStoreMigrationTests` **0** (class absent). Sorted method-set SHA-256 `eab323be548a65055fe86d0a421909b238bdaa33975719b6c3fd50ee02b656ae`, byte-identical to Story 31.1's post-code-review snapshot, so this baseline is continuous with that ledger and there is **no external same-lane delta** to separate. Built-assembly SHA-256 is deliberately **not** recorded: Story 31.1 retired it as unable to establish count agreement. Planned Story 31.2 delta **+6..10** methods, all in the `Deployment` namespace; planned values are not actual evidence. | Matched **2/2** intended creation paths against declared baseline `115d30b59101910d0fd30717f49a5fb7f1782547` using `git status --porcelain` for added working-tree files and `git diff --name-status 115d30b59101910d0fd30717f49a5fb7f1782547` for modifications. The worktree was **clean** at creation — `git status --porcelain` returned no entries — so there are **no named exclusions** and no inherited dirty paths, unlike Story 31.1's nine. `sprint-status.yaml` changed from pre-create SHA-256 `6eb0077b6ee0306f1cc5f396d982849ab614aaee6138b882f4f693be5c25fd88`, carries **partial ownership** (the `31-2-...` row and `last_updated` only), and its working-tree CRLF endings are pre-existing and file-wide. No path was renamed and none returned to its baseline content. |
| 2026-08-01 | correct-course | Applied the approved Sprint Change Proposal 2026-08-01 (Story 31.1 checkpoint split and Epic 31 activation gate), which was triggered when a `dev-story` execution against this story halted at its own Task 0. **Four changes to this story, no implementation:** (1) the **activation gate** in `epics.md` is rebound from "Story 31.1 is `done`" to Story 31.1's documentation checkpoints `C1/C2/C3/C4a/C5a/C6` — the gate's stated purpose is a documented platform whose accepted limitations are on record, which is executably asserted today, while `done` additionally encoded an independent security countersignature that no development on either story can produce, making this story permanently undevelopable for a reason the gate does not state; the countersignature stays open on Story 31.1 as `C4b`/`C5b`/`C7`. (2) The **Owned paths** list in `epics.md` is widened from its single entry, discharging **Open Decision D3** — it contradicted this story's own Implementation-evidence clause, which requires an inventory, a structural proof and a negative proof that cannot live in a Dapr component manifest. (3) A missing **`### Epic AC Verification`** section is added under `## Dev Notes`; the story was registered 2026-07-28 without it while `_bmad/custom/epic-ac-verification.md` requires it and fail-closes `ready-for-dev`. Three of its seven rows are `corrected`, two of them by this same correction. (4) The **testing baseline** is corrected — see the Test count cell. **Open Decisions D1 (`eventstore` scope unprovable by live read) and D2 (`DW 27.3-CR17` repair path) are deliberately not resolved**; both remain routed for a human decision and will surface during development. Status is unchanged at `ready-for-dev`; the story is now developable. No task checkbox was ticked, no owned implementation path was edited, no checkpoint row moved off `pending`, and no cluster, manifest, source, or dependency changed. | Actual phase delta **+0**, cumulative **+0** from the create baseline — this phase added no test. **The create baseline recorded by the `create-story` row is stale and is corrected here rather than rewritten.** Named unit **xUnit test method**; runner, discovery scope, filters and configuration identical to the create baseline, so the subtraction is between comparable discoveries. Exact command `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo` after `DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --configuration Release --disable-build-servers -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` (0 warnings, 0 errors, 18.94s). Observed at HEAD `1d9e9c89`: `Hexalith.Memories.Server.Tests` **2,203**; `Deployment` namespace **61**; `OpenBaoPlatformDocumentationTests` **13**; `ProductionDeploymentArtifactsTests` **9**; `RuntimeSecretStoreMigrationTests` **0** (class still absent, as at creation). Sorted method-set SHA-256 **aacac6f53f49f2a7450345646e76fc7ffde2e896d716f8785ab08457f347f22a**, which is **not** the `eab323be…` value the `create-story` row records. **Named external same-lane delta +3, owner Story 31.1**, landed in commit `a4517654`: `DeployedProfileTable_PinsEveryMeasuredRowByKey`, `OperationalSections_StayBoundToTheirRecordedRemediations` and `SecretShapeGuard_ExcusesALabelledDigestAndReportsAnUnlabelledPaste`, verified with `git diff 115d30b5..HEAD -- tests/Hexalith.Memories.Server.Tests/Deployment/OpenBaoPlatformDocumentationTests.cs` and independently corroborated by Story 31.1's own `code-review` row, which claims exactly those three methods and the same post-patch hash. So **create baseline 2,200 + story delta 0 + external delta +3 = observed 2,203**. Cause, recorded so it is not repeated: commit `a4517654` bundled this story's `create-story` output **and** Story 31.1's post-review test additions, while this story's `baseline_commit` is `115d30b5`, that commit's parent — the baseline was measured one commit before three methods that landed alongside this very story file. The `create-story` row is left intact and its "**no** external same-lane delta" claim is corrected in `### Testing Baseline and Planned Delta`, not overwritten. | Matched **4/4** cumulative story paths against declared baseline `115d30b59101910d0fd30717f49a5fb7f1782547`, using `git diff --name-status 115d30b5…` for tracked paths and `git status --porcelain` for untracked ones. **Two paths joined the owned set this phase:** `_bmad-output/planning-artifacts/epics.md` (**partial ownership** — this story owns only its own Activation-gate and Owned-paths hunks under `### Story 31.2`) and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-1-checkpoint-split-and-epic-31-activation-gate.md` (untracked, new, jointly owned with Story 31.1 and credited in both File Lists). The two already-owned paths are this story file and `_bmad-output/implementation-artifacts/sprint-status.yaml`, which carries **partial ownership** unchanged — its `epic-31` comment block and `last_updated` were edited and **no `development_status` value changed**, so this story remains `ready-for-dev`. No path was renamed and none returned to its baseline content. `git diff --check` is clean; `.md` materializes CRLF and `sprint-status.yaml` LF per `.gitattributes`, verified with `grep -c $'\r$'`. **Named exclusions, none credited here:** everything else in the `115d30b5..worktree` range, which is Story 31.1's completed work (`31-1-…md`, `tests/31-1-openbao-platform-evidence.md`, the seven `sprint-change-proposal-2026-07-28-*` files), the Administrator's BMAD process-policy session (`process-notes/story-creation-lessons.md`, the `spec-*.md` artifacts), and Jérôme Piquot's concurrent Epic 27 / Story 27.3 session (`27-3-…md`, `deferred-work.md`, `architecture.md`, `sprint-change-proposal-2026-07-30*.md`, `sprint-change-proposal-2026-07-31.md`, `sprint-change-proposal-2026-08-01.md`, `22-2-…md`, `26-5-…md`, `tests/27-3-adapter-profile-evidence.md` and its `adapter-profile-runs/` artifact, the `src`/`tests` AccessTelemetry Lifecycle sources, `tests/tooling/production_deployment_evidence/`, the three `tools/*production-deployment*.ps1` scripts) plus the user-owned gitlink `references/Hexalith.Builds`. That set was already dirty when this phase began and is untouched by it. |
| 2026-08-01 | correct-course | **Second `correct-course` row of 2026-08-01**, appended per `story-phase-ledger.md` ("a repeated phase appends another row with that same canonical phase name; it never overwrites an earlier row"). Applied the approved Sprint Change Proposal 2026-08-01 (Story 31.2 reserved `eventstore` scope and deferred-work retarget), settling the two Open Decisions the first proposal deliberately left routed. **D1 resolved — candidates (c)+(a), candidate (b) rejected.** Re-derivation at HEAD `1d9e9c89` made the creation finding stronger: not only does no pod carry app-id `eventstore`, **no manifest in `deploy/kubernetes/` declares one** — `grep -rn 'dapr.io/app-id' deploy/kubernetes/` returns exactly `memories`, `memories-mcp`, `memories-access-telemetry`, `memories-access-telemetry-clock`, while `kubectl -n hexalith-memories get sa,role` shows an `eventstore` ServiceAccount (0 secrets, 12d) and an `eventstore-dapr-secret-reader` Role for a workload that is not declared anywhere. EventStore is linked as the `references/Hexalith.EventStore` submodule and the `src/Hexalith.Memories.EventStore` project, not deployed as a Dapr app. AC1 is amended in this file **and** in `epics.md` to require the `memories` scope by live scoped read and the reserved `eventstore` scope structurally — declared presence plus a demonstrated denial from a non-scoped app-id — with owner `Memories Maintainer` and the reopen trigger "an `eventstore`-app-id workload is deployed". Candidate (b), a throwaway `eventstore` sidecar, was rejected as fabricating a workload the topology does not have. Task 4 and checkpoint C1 restated accordingly; the "never describe the structural proof as a live read" guard is retained and strengthened. **D2 resolved — the question was moot and the target was stale.** `DW 27.3-CR17` was **split on 2026-07-29** by Story 27.3's chunk-2 code review, one day after this story was created, and now covers only the Story 27.3 / checkpoint C2 arm; this story's arm moved to `DW 27.3-CR29`, joined by `DW 27.3-CR30`. Both are `open` and both name Story 31.2 as owner. Separately, the disposable-`kind` repair D2 asked about was already decided by the Administrator on 2026-07-28 and executed inside Story 27.3 (verification-scoped `secretstores.kubernetes` substitution with a validator-enforced `secret-store-substitution.json` disclosure, commits `564d5d56` and `64434e57`). Task 8, checkpoint C5, the Scope Boundary, the Expected File Ownership table and the References are retargeted to `CR29`/`CR30`, and `tools/verify-production-deployment.ps1` moves from conditionally-owned to **`Preserve` / out of scope**. **D3** is also marked resolved, pointing at the first 2026-08-01 proposal that widened the `epics.md` Owned paths. No unrouted decision remains outstanding. `deferred-work.md` is **not** edited by this phase: `CR29` and `CR30` are dispositioned by Task 8 at discharge time with re-derived evidence. No implementation, manifest, source, cluster, or dependency change occurred; status is unchanged at `ready-for-dev`. | Actual phase delta **+0**, cumulative **+0** from the create baseline. This phase changed no test source, so no rebuild was required; discovery was nevertheless re-run rather than asserted. Named unit **xUnit test method**; runner, discovery scope, filters and configuration identical to the preceding `correct-course` row, so the comparison is between comparable discoveries. Exact command `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Release/net10.0/Hexalith.Memories.Server.Tests.dll -list methods -noLogo`. Observed totals before -> after: `Hexalith.Memories.Server.Tests` **2,203 -> 2,203**; `Deployment` namespace **61 -> 61**. Sorted method-set SHA-256 **aacac6f53f49f2a7450345646e76fc7ffde2e896d716f8785ab08457f347f22a -> aacac6f53f49f2a7450345646e76fc7ffde2e896d716f8785ab08457f347f22a**, byte-identical in both directions and to the value the preceding row records. The reconciliation carried forward is unchanged: **create baseline 2,200 + story delta 0 + external delta +3 (owner Story 31.1, commit `a4517654`) = observed 2,203**. No test was executed this phase and none is claimed; the `C4a`/`C5a` guard run cited in the preceding row belongs to Story 31.1 and is not re-credited here. | Matched **5/5** cumulative story paths against declared baseline `115d30b59101910d0fd30717f49a5fb7f1782547`, using `git diff --name-status 115d30b5…` for tracked paths and `git status --porcelain` for untracked ones. **One path joined the owned set this phase:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-01-story-31-2-reserved-eventstore-scope-and-deferred-work-retarget.md` (untracked, new), owned solely by Story 31.2. The **two** paths this phase edited that were already owned are this story file and `_bmad-output/planning-artifacts/epics.md` (**partial ownership** — the AC1 `Then` clause and the Implementation-evidence clause under `### Story 31.2`, in addition to the Activation-gate and Owned-paths hunks the first proposal amended). `_bmad-output/implementation-artifacts/sprint-status.yaml` is owned and **unedited** this phase; no `development_status` value changed, so this story remains `ready-for-dev`. `_bmad-output/implementation-artifacts/deferred-work.md` is **not** claimed — it joins at Task 8 discharge, not now. No path was renamed and none returned to its baseline content. `git diff --check` is clean; `.md` materializes CRLF per `.gitattributes`, verified with `grep -c $'\r$'`. **Named exclusions, none credited here:** unchanged from the preceding row — Story 31.1's completed work, the Administrator's BMAD process-policy artifacts, and Jérôme Piquot's concurrent Epic 27 / Story 27.3 working-tree set, plus the user-owned gitlink `references/Hexalith.Builds`. |
