# Adversarial divergence review

**Target:** `../../architecture.md`  
**Lens:** Independently built units one level down that can follow the written architecture yet fail to interoperate  
**Repository evidence date:** 2026-09-09

## Verdict

**FAIL — do not treat this document as a consistency contract.** It contains detailed implementation guidance, but several load-bearing seams have two incompatible written answers. The repository already exhibits four of the forks: MemoryUnit ingestion bypasses the EventStore command boundary used by Case/Tenant mutations; Redis consumers use shared process-wide credentials while the document names per-tenant principals as the isolation target; actor callers use tenant-only IDs while D15 prescribes type-prefixed IDs; and the Server owns direct Conversation API enrichment while D27 assigns that ownership to a Python service. Hybrid search also silently drops the graph axis in the exact no-seed path the current product contract calls three-axis.

The findings below are counterexamples, not style objections. In each pair, the units can point to explicit text in `architecture.md` for their choice and still be mutually incompatible.

## Findings

### Critical

#### C1. Per-tenant Redis principals and process-wide keyed connections cannot both own tenant routing

- **Divergence class:** security/isolation; shared-data ownership; deployment wiring
- **Architecture evidence:** physical isolation is defined as per-tenant ACL users resolved through tenant-scoped backend routing (`architecture.md:71`, `architecture.md:76`, `architecture.md:280-284`, D29 at `architecture.md:603`). Yet D30's binding construction gives product code the process-wide keyed `"redis"`/`"falkordb"` connections (`architecture.md:646-648`), and the document concedes that D29 enforcement is not implemented (`architecture.md:617`).
- **Independent unit A:** `TenantProvisioningWorkflow` creates tenant A's ACL user and returns tenant-specific credentials/connection details from the documented `TenantInfrastructureResolver`.
- **Independent unit B:** `SyntacticSearchService` and index activities inject the documented keyed `"redis"` singleton and select an index/key by tenant ID; they never resolve a tenant-specific principal.
- **Incompatibility:** if unit B uses the process-wide credential, the ACL is not the access-control boundary and a routing/key bug retains cross-tenant authority. If the shared credential is removed in favor of tenant ACLs, unit B cannot access any tenant because its constructor has no tenant-aware connection resolver. Provisioning and consumption have incompatible connection-ownership models.
- **Current-code proof:** `ServiceDefaults.AddKeyedRedisConnections` registers one singleton per backend from one connection string (`src/Hexalith.Memories.ServiceDefaults/Extensions.cs:76-100`); `SyntacticSearchService` injects that singleton and calls `GetDatabase()` (`src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs:49-82`); `IndexSyntacticActivity` does the same (`src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs:25-54`). The Kubernetes Server receives one shared Redis password and one shared connection string (`deploy/kubernetes/base/server-deployment.yaml:46-59`). No tenant-aware connection seam exists in these consumers.
- **Consequence:** the hard NFR8 boundary degrades to naming/filter correctness; independently delivered ACL provisioning can also break every current search/index consumer.
- **Disposition:** **discuss**, then update. Choose one enforceable model and name the owning seam. For D29, require every Redis operation to obtain a tenant-scoped connection/principal through one resolver; define credential caching/rotation, transaction lifetime, failure behavior, and the migration cutover from the shared credential. Until that lands, classify D29 as an open blocker rather than a validated decision.

#### C2. MemoryUnit creation has no single authoritative mutation path

- **Divergence class:** state mutation; source-of-truth ownership; recovery
- **Architecture evidence:** the main consistency rule says Case and MemoryUnit commands must be accepted by EventStore before projection (`architecture.md:98`; D3 at `architecture.md:582`). The phase rule simultaneously describes MVP as having “no EventStore integration” (`architecture.md:192`). The normative data flow sends both event and content ingress straight to `IngestionWorkflow`, whose listed steps fan out to Redis/FalkorDB without an EventStore command/ack step (`architecture.md:1598-1620`).
- **Independent unit A:** a Case/Tenant mutation service follows D3: submit a domain command to EventStore, await acceptance, then schedule a projection workflow.
- **Independent unit B:** the ingestion endpoint follows the document's data-flow section: schedule `IngestionWorkflow`, then write the three projections and mark the unit indexed.
- **Incompatibility:** unit A can rebuild its state from EventStore; unit B has no authoritative MemoryUnit commit to replay. After projection loss, EventStore contains Case/Tenant history but not the content-ingested MemoryUnit state the architecture says it owns. “Indexed” also means different things: projection completion for unit B versus post-commit projection completion for unit A.
- **Current-code proof:** `CaseService.CreateCaseAsync` accepts the command and only then schedules projection (`src/Hexalith.Memories.Server/Cases/CaseService.cs:84-99`). In contrast, the content-ingest endpoint schedules the ingestion workflow directly (`src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:137-142`), and URL ingestion does the same (`src/Hexalith.Memories.Server/Endpoints/IngestionEndpoints.cs:303-320`). `IngestionWorkflow` builds projection inputs and directly starts syntactic, semantic, and graph indexing (`src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:304-356`); it has no `IMemoriesCommandStore` boundary. The domain project has a deletion command but no MemoryUnit creation command (`src/Hexalith.Memories.EventStore/Domain/Commands/DeleteMemoryUnitCommand.cs:11`).
- **Consequence:** the claimed EventStore rebuild path is incomplete for the system's central entity; two teams can ship “compliant” mutations with different durability and ordering semantics.
- **Disposition:** **discuss**. Bind the exact MemoryUnit command/event, the point at which `pending` becomes durable, the acknowledgement required before workflow projection, and how EventStore-originated events differ from creating a Memories MemoryUnit aggregate. Then align data-flow diagrams and code. This cannot be safely autofixed as prose because it changes domain semantics and recovery behavior.

### High

#### H1. DAPR actor identity has two incompatible formulas

- **Divergence class:** state ownership; rate limiting; protocol identity
- **Architecture evidence:** D15 specifies actor identity as `{actorType}-{tenantId}` (`architecture.md:557`) and the naming section repeats it (`architecture.md:775-778`). D24 instead says `Actor ID = tenant ID` (`architecture.md:598`), while the proxy example constructs `new ActorId(tenantId)` and passes actor type separately (`architecture.md:1003-1008`).
- **Independent unit A:** a workflow activity follows D15 and calls `EmbeddingRateLimiterActor` with actor ID `EmbeddingRateLimiterActor-tenant-a`.
- **Independent unit B:** a tenant-configuration or embedding component follows D24/the proxy example and calls the same actor type with actor ID `tenant-a`.
- **Incompatibility:** DAPR keys virtual actor state by the `(actor type, actor ID)` pair. These calls activate different actors and maintain different rate budgets/statistics for the same tenant. A caller can bypass the budget seen by another caller without violating one of the document's explicit examples.
- **Current-code proof:** both embedding activities use tenant-only IDs (`src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:102-107`; `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateChunkEmbeddingsActivity.cs:114-117`). Case counters use a separate composite `tenantId:caseId` convention (`src/Hexalith.Memories.Server/Activities/Ingestion/UpdateCaseIngestionCounterActivity.cs:43-45`), also absent from D15.
- **Consequence:** split actor state, ineffective throttling, and inconsistent operator metrics.
- **Disposition:** **autofix**. Ratify current DAPR semantics: actor type is the separate proxy argument; tenant-scoped actors use `ActorId = tenantId`; case-scoped actors use one explicitly documented, escaped composite identity. Remove D15's type prefix and the `ingestion-pipeline-*` example.

#### H2. AI enrichment has two owners and no versioned cross-language contract

- **Divergence class:** component ownership; interface/protocol/version; shared-data shape
- **Architecture evidence:** D27 assigns AI enrichment, NLP, and causal inference to a Python `ai-agent` service (`architecture.md:600-602`), and the service-invocation example calls its `/enrich` operation (`architecture.md:1133-1152`). A different normative example implements `ExtractMetadataActivity` in the C# Server and calls `DaprConversationClient` directly (`architecture.md:1061-1094`). The only C#/Python compatibility rule is manual model mirroring with camel-case JSON (`architecture.md:1232`, `architecture.md:1299`); required/optional fields, unknown enums/fields, schema negotiation, error envelope, retry ownership, and idempotency are not bound.
- **Independent unit A:** the Server team implements the documented C# Conversation activity and owns prompt, parsing, retry, and `MetadataExtractionResult`.
- **Independent unit B:** the Python team implements the documented `/enrich` service with Pydantic models and owns the same metadata extraction and causal inference.
- **Incompatibility:** the workflow can invoke both and duplicate non-deterministic enrichment, or invoke one while deployment only includes the other. Even if the routing choice is guessed, “mirror” allows Python to reject a C#-added required field, ignore an enum it does not know, or return a shape the C# serializer cannot bind. `Contracts.V1` namespaces do not version the `/enrich` route or negotiate compatibility.
- **Current-code proof:** the repository chose the C# direct path: `GenerateNaturalLanguageDescriptionActivity` owns `DaprConversationClient` and directly invokes `ConverseAsync` (`src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs:34-36`, `:65-87`, `:124-148`). AppHost wires the Conversation component directly for that activity (`src/Hexalith.Memories.AppHost/Program.cs:263-274`). There is no `services/ai-agent/` directory or `ai-agent` AppHost resource.
- **Consequence:** duplicate or missing enrichment, divergent provenance/confidence data, and brittle cross-language upgrades.
- **Disposition:** **discuss**. Ratify one owner per enrichment capability. If Python remains, bind a versioned `/enrich` request/response/error contract (prefer generated schema), compatibility policy, idempotency key, timeout/retry owner, and deployment requirement. If current C# ownership is intentional, retire D27/D28's Python topology and examples.

#### H3. “Three-axis hybrid” has no graph-seed contract and silently becomes two-axis

- **Divergence class:** query protocol; algorithmic behavior; benchmark comparability
- **Architecture evidence:** three-axis RRF is called the architectural center and the default live graph weight is `0.35` (`architecture.md:94-96`). D2 says the graph scorer is enabled by default for thesis validation (`architecture.md:194-199`), but neither D2 nor the search data flow defines how a text query obtains graph start nodes (`architecture.md:1622-1626`).
- **Independent unit A:** a CLI/MCP client sends `axis=hybrid` with query text and no graph node, reasonably treating “hybrid” and the default weights as all available axes.
- **Independent unit B:** a search/benchmark implementation requires an explicit node and either skips graph or takes a hand-authored seed from its fixture.
- **Incompatibility:** the same “hybrid” request is two-axis in production and three-axis in benchmark/test contexts; a result can be reported non-degraded even though the graph contribution silently disappeared. Scores and thesis-gate claims are not comparable.
- **Current-code proof:** the endpoint exposes separate optional `startNodeId`/`graphStartNodeId` parameters (`src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs:71-89`) and passes null through when neither exists (`src/Hexalith.Memories.Server/Endpoints/SearchEndpoints.cs:414-416`, `:489-501`). `HybridSearchService` explicitly treats a null seed as “skip” (`src/Hexalith.Memories.Server/Search/HybridSearchService.cs:55-71`, `:168-187`) and omits skipped axes from degradation accounting (`:111-118`, `:233-267`). The current PRD has since bound auto-seeding from the top five syntactic plus top five semantic candidates (`_bmad-output/planning-artifacts/prd.md:162`, `:1011`), confirming the architecture omission is material.
- **Consequence:** a supposedly three-axis Gate 1 can test a different system than users invoke, or can be gamed through label-informed fixture seeds.
- **Disposition:** **autofix** the architecture from the adopted PRD decision, then implement. Specify deterministic seed selection, candidate window, dedup/tie ordering, maximum traversal depth, explicit-seed override, and whether failure to seed is degraded. Keep the implementation status visible until production and benchmark share the same path.

#### H4. Authentication is both an implemented MVP invariant and a Phase 1.5 non-gate

- **Divergence class:** security; deployment/operations; interface availability
- **Architecture evidence:** Security Architecture says JWT fallback authorization and tenant endpoint filters are implemented (`architecture.md:217-220`; D8 at `architecture.md:587`). The gate table says `TenantAuthorizationMiddleware` is not gate-blocking and Phase 1.5 (`architecture.md:315`), and requirements coverage also classifies NFR11 ingress authentication as Phase 1.5 (`architecture.md:1664-1666`).
- **Independent unit A:** the Server team follows Security Architecture and requires a JWT on every product endpoint except named health/DAPR infrastructure routes.
- **Independent unit B:** the MVP deployment/onboarding team follows Gate-Blocking/Coverage and omits the identity provider and JWT settings until Phase 1.5.
- **Incompatibility:** the Server cannot start or every product request is unauthorized in the deployment that the same architecture calls MVP-complete.
- **Current-code proof:** Server registration validates authentication options on startup and installs an authenticated fallback policy (`src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:70-78`, `:102-107`). Validation fails unless authority/signing key, issuer, audience, tenant claim, and algorithms are configured (`src/Hexalith.Memories.Server/Authentication/ValidateServerAuthenticationOptions.cs:16-52`). The Kubernetes manifest already supplies OIDC settings (`deploy/kubernetes/base/server-deployment.yaml:72-97`).
- **Consequence:** independently built deployment and application units do not compose; worse, a team may weaken the fallback policy to make the stale MVP onboarding path pass.
- **Disposition:** **autofix**. Current code and the current PRD ratify NFR11 as MVP. Remove the Phase 1.5/non-gate classifications and bind the named anonymous route allowlist plus required local-development identity/bootstrap path.

### Medium

#### M1. Gate 3 names Docker Compose while the living topology is Aspire AppHost

- **Divergence class:** operations; onboarding acceptance boundary
- **Architecture evidence:** the topology says one `dotnet run --project Hexalith.Memories.AppHost` command boots everything (`architecture.md:270`), while Gate 3 requires a “Docker Compose single-command boot” (`architecture.md:313`, `architecture.md:387`). The build-order table calls AppHost a “Docker Compose equiv” (`architecture.md:519`) without deciding which artifact the gate measures.
- **Independent unit A:** an onboarding/test team authors Gate 3 around `docker compose up` and Compose health/dependency semantics.
- **Independent unit B:** an operations team authors resources, secrets seeding, DAPR components, and readiness ordering only in AppHost.
- **Incompatibility:** the test targets an artifact that does not carry the living OpenBao/EventStore/DAPR topology, while the real boot path is never measured by the named gate.
- **Current-code proof:** the repository has no root `docker-compose*.yml`; the shipped quickstart uses AppHost (`README.md:34-40`, `docs/dev/quickstart.md:48-54`). AppHost includes runtime resources well beyond the five-container seed listed in the architecture, including OpenBao and EventStore (`src/Hexalith.Memories.AppHost/Program.cs:249-254`, `:283-303`).
- **Consequence:** false Gate 3 evidence and two deployment descriptions that drift independently.
- **Disposition:** **autofix**. Name AppHost as the only Phase 1 onboarding artifact and define the exact stopwatch path. If Compose is still a deployment deliverable, assign it a separate purpose, owner, parity test, and revisit condition.

## Disposition summary

| ID | Severity | Disposition | Gate effect |
|---|---|---|---|
| C1 | Critical | Discuss, then update | Blocks NFR8 / tenant isolation sign-off |
| C2 | Critical | Discuss | Blocks durability/rebuild and source-of-truth sign-off |
| H1 | High | Autofix | Split actor state / rate-limit bypass risk |
| H2 | High | Discuss | AI ownership and cross-language contract unresolved |
| H3 | High | Autofix architecture; implementation follow-up | Blocks truthful three-axis Gate 1 |
| H4 | High | Autofix | MVP application/deployment cannot compose from current classifications |
| M1 | Medium | Autofix | Gate 3 targets the wrong operational artifact |

## Minimum sign-off conditions

1. Resolve C1 and C2 as explicit invariants, not “targets,” examples, or phase prose.
2. Normalize actor identity and authentication phase in every registry/table/example that repeats them.
3. Pick AI ownership and publish a versioned cross-service contract if the Python boundary survives.
4. Import the adopted graph auto-seeding rule and make production and benchmark use the same path.
5. Make AppHost the named onboarding gate, or provide a real parity-controlled Compose artifact.
