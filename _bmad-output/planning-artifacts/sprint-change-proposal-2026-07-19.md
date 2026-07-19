---
project: memories
date: 2026-07-19
status: approved
change_scope: moderate
review_mode: incremental
prepared_for: Administrator
approved_by: Administrator
trigger_story: 27.3
---

# Sprint Change Proposal — OpenBao-First Aspire Secret Management

## 1. Issue Summary

Story 27.3 migrated the production-shaped Kubernetes deployment's two Dapr secret-store components to
OpenBao. Repository and live Aspire inspection then exposed a topology mismatch:

- Production `secretstore` and `access-telemetry-secrets` components use OpenBao through
  `secretstores.hashicorp.vault`, with separate prefixes and read-only policies.
- The root Aspire AppHost still generates both components as `secretstores.local.file` and has no OpenBao
  resource or initialization dependency.
- The reusable `Hexalith.Memories.Aspire` APIs hard-code `secretstores.local.file`, preventing consumers
  from supplying an OpenBao-backed component without bypassing those APIs.
- The reusable access-telemetry composition does not attach its secret-store component to the clock Dapr
  sidecar even though the clock calls the Dapr Secrets API.
- The generic `deploy/dapr/components/secretstore.yaml` template still selects
  `secretstores.kubernetes`, and an existing regression test requires that stale provider choice.

The product-code boundary is already substantially correct. Embedding resolution, access-telemetry
lifecycle bootstrap, and the access-telemetry clock call `DaprClient.GetSecretAsync`; they do not access
OpenBao directly. The required correction is therefore primarily Aspire composition, Dapr component
configuration, bootstrap sequencing, planning alignment, and observable verification.

### Evidence

- `src/Hexalith.Memories.AppHost/Program.cs` registers `secretstore` and
  `access-telemetry-secrets` as `secretstores.local.file`.
- `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs` and
  `HexalithMemoriesAccessTelemetryExtensions.cs` hard-code the same provider.
- `src/Hexalith.Memories.Server/Ingestion/EmbeddingSecretStore.cs`,
  `AccessTelemetryLifecycleBootstrapService.cs`, and
  `src/Hexalith.Memories.AccessTelemetry.Clock/Program.cs` resolve values through Dapr.
- `deploy/kubernetes/base/dapr/secretstore.yaml` and `access-telemetry-secrets.yaml` already use the
  OpenBao-compatible Dapr component type.
- A live Aspire run built with zero warnings and errors; both secret components and their consumers became
  healthy, but the described resource graph contained no OpenBao resource and identified the two local
  secret-store components.
- Current operations evidence proves production OpenBao readiness, scoped reads, cross-prefix denial, TLS
  verification, and restart recovery. That work remains owned by Story 27.3 and is not rolled back.

## 2. Impact Analysis

### Epic and story impact

- **Epic 26:** Remains completed. Its production deployment work is preserved.
- **Epic 27 / Story 27.3:** Remains in progress under its existing retention scope. Its OpenBao production
  evidence triggered this correction but is not expanded or reopened by this proposal.
- **Epic 28:** Remains backlog and activation-gated by EventStore owner approval.
- **Epic 29:** New cross-cutting epic for OpenBao-first Dapr secret management. It is prioritized after the
  active Story 27.3 work and before additional secret-consuming capabilities.
- **Story 1.4:** Historical embedding acceptance text is aligned with the OpenBao-backed Dapr boundary;
  Story 29.2 owns new verification, so Story 1.4 is not reopened.
- **Story 7.1:** CLI configuration text is corrected so application/provider secrets are not presented as
  CLI fallback configuration.
- **Story 15.6:** Its local-file/Kubernetes secret-store provider decision is explicitly superseded without
  rewriting the remainder of the completed scaffolding story.

No existing epic becomes obsolete, and no rollback or renumbering is required.

### Artifact conflicts

- **PRD:** The embedding API-key source, configuration layering, and NFR9 still authorize direct .NET User
  Secrets access for local runtime secrets. They require an OpenBao-first Dapr contract and narrowly
  documented bootstrap exceptions.
- **Architecture:** The current `Local file (dev) / DAPR Secrets API (deployed)` statement conflicts with
  the desired provider-neutral application boundary. Decision D31 is needed to define OpenBao ownership,
  component isolation, bootstrap exceptions, and evidence.
- **UX:** No interface, flow, wireframe, interaction, localization, or accessibility change is required.
- **Deployment and component templates:** Kubernetes production components already follow the intended
  provider boundary. Standalone/local templates and their tests require reconciliation.
- **Operations documentation:** Embedding-provider guidance still instructs developers to use repo-root
  `secrets.json`. It must describe OpenBao initialization and Dapr-only application access instead.
- **Testing:** Existing Dapr-client unit tests remain valuable. New topology and integration evidence must
  prove the provider, resource references, isolation, readiness, restart behavior, and redaction boundary.

### Technical impact

The implementation is expected to affect:

- `src/Hexalith.Memories.AppHost/Program.cs` and supporting AppHost composition helpers;
- `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs`;
- `src/Hexalith.Memories.Aspire/HexalithMemoriesAccessTelemetryExtensions.cs`;
- local/standalone Dapr component templates;
- AppHost model, Aspire integration, Dapr component, and structural dependency tests; and
- embedding-provider and OpenBao operations documentation.

Product services must remain provider-neutral. No OpenBao package, HTTP client, endpoint construction, or
credential handling enters `Server`, `Cli`, `Mcp`, `Web`, or `Client.Rest`.

Existing dirty Story 27.3 production manifests, evidence, documentation, tests, sprint status, and root
submodule state belong to concurrent/user work. Implementation must preserve and integrate with them
without reverting, overwriting, staging, or committing them.

## 3. Recommended Approach

Use **Direct Adjustment** by adding Epic 29 and aligning the affected planning contracts.

- **Effort:** Medium
- **Risk:** Medium
- **Timeline impact:** Two focused stories after the active Story 27.3 work
- **MVP impact:** None; the MVP thesis and feature scope remain unchanged
- **Release impact:** Improves local/deployed topology consistency without rolling back production OpenBao

### Alternatives considered

1. **Rollback production OpenBao:** Not viable. It would restore Kubernetes as the application-secret
   provider and discard verified isolation and recovery work.
2. **Add the change to Epic 27:** Rejected. Embedding secrets and the public Aspire package exceed the
   access-telemetry retention boundary.
3. **Reopen Epic 26 or Story 15.6:** Rejected. Both are completed historical work; supersession and a new
   epic preserve their records.
4. **Review or reduce MVP scope:** Unnecessary. The correction changes secret-provider composition, not
   product capability.

### High-level implementation sequence

1. Apply the approved PRD, epic, and architecture edits and add Epic 29 backlog entries.
2. Create Story 29.1 and implement a pinned, health-checked, development-safe OpenBao AppHost resource,
   initialization boundary, isolated policies/prefixes, and OpenBao-backed Dapr components.
3. Create Story 29.2 and make the reusable Aspire APIs accept externally provisioned/provider-neutral Dapr
   secret-store resources; attach those resources to every consuming sidecar.
4. Reconcile standalone component templates, tests, and operator/developer documentation.
5. Run structural, AppHost-model, focused unit, and live Aspire integration verification without emitting
   secret values.

## 4. Detailed Change Proposals

The following edits were reviewed and approved individually by Administrator in incremental mode.

### 4.1 Add Epic 29

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**Old:** The backlog ends with Epic 28 and has no work item covering OpenBao in Aspire-hosted environments.

**New:**

```markdown
## Epic 29: OpenBao-First Dapr Secret Management

Aspire-hosted services resolve application secrets exclusively through Dapr
secret-store components backed by OpenBao. Kubernetes Secrets remain permitted
only for unavoidable bootstrap credentials or direct pod inputs that Dapr
cannot inject.

### Story 29.1: OpenBao-Backed AppHost Secret Topology

As a developer and operator,
I want the Aspire AppHost to provision and initialize OpenBao-backed Dapr
secret stores,
So that local and deployed application code use the same provider-neutral
secret-access boundary.

Acceptance criteria:

- AppHost adds a pinned, health-checked OpenBao resource with a safe
  development profile that cannot silently become a production deployment.
- `secretstore` and `access-telemetry-secrets` use
  `secretstores.hashicorp.vault`, the Dapr component type supporting OpenBao.
- The stores use separate least-privilege policies and secret prefixes.
- Resources consuming secrets wait for OpenBao initialization and receive the
  corresponding Dapr component.
- Application secret payloads are not stored in local-file or Kubernetes
  secret-store components.
- Local bootstrap material uses Aspire secret parameters or protected temporary
  files; Kubernetes Secrets are allowed only for required deployed bootstrap
  tokens and CA certificates.
- Secrets never appear in source control, configuration, logs, diagnostics, or
  Aspire model output.
- Integration evidence proves successful Dapr secret reads, cross-prefix
  denial, health, and restart recovery.

### Story 29.2: Provider-Neutral Aspire Composition and Secret Verification

As an Aspire integration consumer,
I want the reusable Memories Aspire APIs to accept externally provisioned Dapr
secret-store resources,
So that consumers can use OpenBao without product code depending on OpenBao.

Acceptance criteria:

- Reusable Aspire extensions no longer hard-code `secretstores.local.file`.
- Server, access-telemetry lifecycle, and clock sidecars reference their required
  secret-store components.
- Embedding, lifecycle bootstrap, and clock code continue to retrieve secrets
  through `DaprClient.GetSecretAsync`.
- Product projects contain no OpenBao SDK, HTTP client, endpoint, or provider
  credentials.
- Standalone Dapr templates, tests, and operations documentation follow the
  OpenBao-first rule and document every remaining Kubernetes Secret exception.
- Automated topology and integration tests prove both Dapr secret components
  resolve values from OpenBao without exposing secret values.
```

### 4.2 Strengthen the PRD secret contract

**Artifact:** `_bmad-output/planning-artifacts/prd.md`

**Embedding-provider source — old:**

```markdown
| `apiKey` | Provider API key | .NET User Secrets (local dev), DAPR Secrets API (deployed) |
```

**Embedding-provider source — new:**

```markdown
| `apiKey` | Provider API key reference | DAPR Secrets API backed by OpenBao |
```

**Configuration layering — old:**

```markdown
4. DAPR Secrets API (deployed environments — for sensitive values: API tokens, embedding provider keys)
5. .NET User Secrets (local development — for sensitive values)
6. DAPR configuration (sidecar discovery, app-id)
```

**Configuration layering — new:**

```markdown
4. DAPR Secrets API backed by OpenBao for embedding, LLM, and application
   runtime secrets
5. DAPR configuration for sidecar discovery, app-id, and non-secret component
   settings

Sensitive values are not resolved through configuration fallback. Product
services retrieve them through DAPR secret-store components. Aspire secret
parameters or .NET User Secrets may supply protected local bootstrap or
one-time seeding inputs, but product services must not read them as an
alternative runtime secret provider. Kubernetes Secrets are permitted only
where required for OpenBao bootstrap material or direct pod inputs that DAPR
cannot provide.
```

**NFR9 — old:**

```markdown
| **NFR9** | Embedding provider API keys stored in secure secret management
(.NET User Secrets for local dev, DAPR Secrets API for deployed) — never in
config files or environment variables in production | Code review + secret
scanning in CI | Ongoing |
```

**NFR9 — new:**

```markdown
| **NFR9** | Product services retrieve embedding-provider and other application
runtime secrets exclusively through the DAPR Secrets API, backed by OpenBao in
Aspire and deployed environments. Secret values are never stored in application
configuration or ordinary environment variables. Kubernetes Secrets are
restricted to documented, unavoidable OpenBao bootstrap credentials or direct
pod inputs outside the DAPR secret-store boundary. | Structural dependency
tests, secret scanning, AppHost topology tests, and integration tests proving
DAPR reads from OpenBao without secret disclosure | Ongoing |
```

### 4.3 Align existing story contracts

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**Story 1.4 — old:**

```markdown
**Given** the embedding API key is configured
**When** the system accesses it
**Then** it reads from DAPR Secrets API (deployed) or .NET User Secrets (local dev)
**And** the key is never stored in config files or environment variables
```

**Story 1.4 — new:**

```markdown
**Given** an embedding API key reference is configured
**When** the Server requires the secret
**Then** it retrieves the value through DAPR Secrets API component `secretstore`
**And** the component is backed by OpenBao in Aspire and deployed topologies
**And** tenant configuration contains only the secret name
**And** product code has no direct dependency on OpenBao, .NET User Secrets,
Kubernetes Secrets, or another provider-specific secret API
**And** the secret value is never written to configuration, ordinary
environment variables, logs, traces, or API responses

**Supersession note:** Epic 29 owns implementation and observable verification
of this strengthened secret-provider contract. Story 1.4 remains historical
completed work and is not reopened.
```

**Story 7.1 — old:**

```markdown
**Then** configuration layering is respected (precedence high to low):
command-line flags → environment variables (`HEXALITH_MEMORIES_*`) → config
file (`~/.hexalith/memories.json` or project-local) → DAPR Secrets API →
.NET User Secrets → DAPR configuration (NFR23)
```

**Story 7.1 — new:**

```markdown
**Then** non-secret CLI configuration layering is respected (precedence high
to low): command-line flags → environment variables
(`HEXALITH_MEMORIES_*`) → config file
(`~/.hexalith/memories.json` or project-local) → DAPR configuration (NFR23)
**And** application and provider secrets are not CLI configuration
**And** those secrets remain behind the Server's DAPR secret-store boundary
**And** credentials required directly by the CLI use the protected mechanism
defined by the selected execution environment and are never persisted in the
CLI config file
```

### 4.4 Add architecture decision D31

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`

**Dapr secret-store description — old:**

```markdown
- **Secrets:** Local file (dev) / DAPR Secrets API (deployed) — manages both
  embedding API keys and LLM provider keys
```

**Dapr secret-store description — new:**

```markdown
- **Secrets:** DAPR Secrets API in Aspire and deployed environments, backed by
  OpenBao through `secretstores.hashicorp.vault`. Separate `secretstore` and
  `access-telemetry-secrets` components isolate runtime and access-telemetry
  prefixes. Product code depends only on DAPR and never on OpenBao directly.
```

**New decision-table row:**

```markdown
| D31 | OpenBao-first DAPR secret provider | Application runtime secrets are
resolved through DAPR secret-store components backed by OpenBao. Local-file and
Kubernetes secret stores are not application-secret providers. Aspire secret
parameters or protected files may bootstrap and seed local OpenBao. Kubernetes
Secrets are permitted only for required OpenBao tokens/CA material or direct
pod inputs that DAPR cannot inject; every exception must be documented and
tested. | Operational readiness |
```

**New D31 detail:**

```markdown
#### D31 — OpenBao-first DAPR secret provider

**Invariant.** Product services retrieve application secrets exclusively
through DAPR Secrets API. They do not use an OpenBao SDK, construct an OpenBao
endpoint, read Kubernetes Secrets, or resolve application secrets directly
from .NET User Secrets.

**Component boundaries.**

| DAPR component | OpenBao prefix | Consumers |
|---|---|---|
| `secretstore` | `secret/hexalith/memories/runtime` | Memories Server and components resolving embedding or LLM secrets |
| `access-telemetry-secrets` | `secret/hexalith/memories/access-telemetry` | Memories Server, access-telemetry lifecycle, and clock |

Each component uses a distinct read-only policy. Cross-prefix reads fail
closed.

**Aspire topology.** The AppHost owns the OpenBao resource, health and
initialization sequencing, DAPR component generation, protected bootstrap
inputs, and secret seeding. Consumers wait for initialization and reference
only their required DAPR components. A development-mode OpenBao profile must
be explicit and must not silently publish as a production topology.

**Bootstrap exception.** The DAPR component must authenticate before it can
read OpenBao. Protected Aspire parameters or temporary credential files are
allowed locally. In Kubernetes, narrowly scoped Secrets may hold only required
OpenBao bootstrap tokens and CA certificates. Direct pod inputs may remain
Kubernetes Secrets only where DAPR cannot supply them; migrating those inputs
requires a separately approved Agent Injector or CSI design.

**Security evidence.** Verification must prove successful DAPR reads,
cross-prefix denial, restart recovery, absence of provider-specific product
dependencies, and secret-safe logs and diagnostics.
```

### 4.5 Supersede Story 15.6's obsolete provider rule

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

Retain Story 15.6's historical acceptance criterion and append:

```markdown
**Supersession note:** D31 and Epic 29 supersede Story 15.6 only for the
`secretstore.yaml` provider decision. A secret-store template used by an
Aspire or deployed runtime must use an OpenBao-backed DAPR component rather
than `secretstores.local.file` or `secretstores.kubernetes`. Kubernetes
Secrets may supply only documented bootstrap tokens/CA material or unavoidable
direct pod inputs. The statestore and conversation-component requirements
remain unchanged, and Story 15.6 remains historical completed work.
```

## 5. Implementation Handoff

**Classification:** Moderate — backlog reorganization plus coordinated architecture and implementation
work.

### Product Owner / Developer

- Apply the approved `epics.md` and PRD changes.
- Add `epic-29`, Story 29.1, and Story 29.2 to `sprint-status.yaml` as backlog work without disturbing the
  active Story 27.3 entries.
- Create implementation story files before development begins.

### Architect

- Apply D31 and verify it remains consistent with D30 and the existing production OpenBao boundary.
- Review the AppHost development/publish safety boundary and bootstrap exception list.

### Developer

- Implement the root AppHost OpenBao resource and initialization sequence.
- Make reusable Aspire secret composition provider-neutral and attach secret components to all consumers.
- Preserve `DaprClient.GetSecretAsync` as the product-code access path.
- Reconcile templates, tests, and documentation without overwriting concurrent Story 27.3 changes.

### Test and operations reviewers

- Verify OpenBao health and restart behavior without printing secrets.
- Prove both permitted reads and cross-prefix denial through Dapr.
- Confirm Kubernetes Secret usage is limited to explicit bootstrap/direct-pod exceptions.
- Confirm AppHost diagnostics, logs, traces, and errors contain no secret values.

### Success criteria

- The Aspire resource graph contains a pinned, health-checked OpenBao resource and initialization gate.
- Both runtime Dapr secret components use `secretstores.hashicorp.vault` and isolated policies/prefixes.
- No AppHost runtime secret-store component uses `secretstores.local.file` or
  `secretstores.kubernetes` for application secret payloads.
- Server, lifecycle, and clock sidecars receive their required components.
- Product code retrieves application secrets only through `DaprClient.GetSecretAsync` and contains no
  OpenBao dependency.
- Focused builds, unit tests, topology tests, live Dapr reads/denials, and restart verification pass.
- Secret scanning and redaction checks find no secret disclosure.
- Every retained Kubernetes Secret has a documented bootstrap or direct-pod justification.

## Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-19 | Story 27.3 production OpenBao delta identified as trigger | Complete |
| 2026-07-19 | PRD, epics, architecture, UX, source, deployment, tests, and operations artifacts reviewed | Complete |
| 2026-07-19 | Aspire 13.4.6 AppHost build and live resource-health inspection | Passed; local-file provider drift confirmed |
| 2026-07-19 | Direct Adjustment, rollback, and MVP-review paths evaluated | Direct Adjustment selected |
| 2026-07-19 | Five detailed edit proposals reviewed incrementally | Approved by Administrator |
| 2026-07-19 | Complete Sprint Change Proposal generated and reviewed | Continued by Administrator |
| 2026-07-19 | Complete Sprint Change Proposal approved for implementation | Approved by Administrator |
| 2026-07-19 | PRD, epics, architecture, and sprint-status planning records reconciled | Complete |
| 2026-07-19 | Moderate-scope Product Owner / Developer handoff | Complete |

## Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Triggering story identified: Story 27.3 exposed local/reusable Aspire drift after production OpenBao migration.
- [x] 1.2 Core problem categorized as a new security requirement plus implementation-consistency gap.
- [x] 1.3 Evidence collected from source, templates, tests, operations records, and a live Aspire run.

### 2. Epic impact assessment

- [x] 2.1 Epic 27 remains viable without scope expansion.
- [x] 2.2 New cross-cutting Epic 29 is required.
- [x] 2.3 Remaining planned epics reviewed; Epic 28 remains activation-gated and otherwise unaffected.
- [x] 2.4 No future epic is invalidated.
- [x] 2.5 Epic 29 is prioritized after active Story 27.3 work.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD conflicts identified and approved edits defined.
- [x] 3.2 Architecture conflict identified and D31 defined.
- [N/A] 3.3 UX has no interface or flow impact.
- [x] 3.4 AppHost, Aspire package, Dapr templates, tests, documentation, and deployment boundaries assessed.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable; effort medium, risk medium.
- [N/A] 4.2 Rollback is not viable and would weaken the desired provider boundary.
- [N/A] 4.3 PRD/MVP reduction is unnecessary.
- [x] 4.4 Direct Adjustment selected for sustainability and minimum disruption.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP impact, action sequence, and dependencies documented.
- [x] 5.5 Moderate-scope handoff responsibilities documented.

### 6. Final review and handoff

- [x] 6.1 Applicable analysis checklist items completed.
- [x] 6.2 Proposal checked against repository and runtime evidence.
- [x] 6.3 Complete proposal explicitly approved by Administrator on 2026-07-19.
- [x] 6.4 `sprint-status.yaml` updated with Epic 29 and its two backlog stories while preserving concurrent Story 27.3 edits.
- [x] 6.5 Moderate-scope Product Owner / Developer handoff, sequencing, and success criteria confirmed.
