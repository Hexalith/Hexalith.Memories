# Architecture Spine Review — Security, Multi-Tenant Isolation, and Data Integrity

**VERDICT: FAIL** — three critical and eight high findings; each describes a scenario that a *fully compliant* implementation of the spine as written is permitted to build.

---

## Scope and method

- **Target (read-only):** `_bmad-output/planning-artifacts/architecture/architecture-memories-2026-09-09/ARCHITECTURE-SPINE.md` (324 lines, `status: final`, updated 2026-09-09). All line references below are to that file unless prefixed.
- **Requirement sources re-verified:** `prd.md` (updated 2026-09-12) NFR8–NFR11, NFR16, NFR34, FR38–FR40, FR67, FR71, FR74, FR75, G2/G4, and the Compliance/Erasure prose at `prd.md:546-554`; `addendum.md` (updated 2026-09-12) "Isolation mechanism", "Identity", "Topology and secrets", "Tenant erasure".
- **Prior review re-verified:** `architecture/architecture-memories-2026-09-09/reviews/review-rubric-integrity.md`. Its C1 (erasure vs. immutable history), H1 (case isolation), H5 (inter-service identity), SP-H1/SP-H2/SP-H3 findings are re-checked below; this review does not re-litigate what that review closed, it tests the *closures* adversarially.
- **Standard applied:** an architecture-level finding must name a failure a *compliant* implementation permits — two implementations can both obey every word of the spine and only one is safe. Every finding below states that explicitly.
- **Nothing was built, run, or modified.** No file other than this review was written.

---

## Area verdicts

### 1. Authorization chain (AD-5, AD-7, AD-13) — **FAIL**

AD-5 (`:80-84`) enumerates exactly two derivation paths: "REST/CLI and MCP preserve validated bearer tenant and subject authority" and "trusted internal calls" mapped through the app-ID allowlist. Tracing every ingress the spine itself names:

| Surface | Tenant authority source | Case authority source | Stated? |
| --- | --- | --- | --- |
| REST `/api/v1` | Validated bearer tenant claim | — | Tenant: yes. Case: **no** |
| CLI | Same bearer, `--tenant` names the *requested* tenant only | — | Tenant: yes. Case: **no** |
| MCP | "preserve validated bearer tenant and subject authority" | — | Tenant: yes. Case: **no** |
| Dapr pub/sub ingress `/events/ingest` | **Unstated** — the route appears only in the Conventions table (`:176`) as a "named infrastructure exception"; AD-5 never mentions it | Unstated | **No** (SEC-04) |
| Internal service-to-service | App ID → allowlist → `system:*` + tenant grant | — | Yes, but ungoverned (SEC-07) |
| Background workflow / activity / repair / replay / migration | **Unstated** — no caller exists; AD-4 captures "immutable non-secret configuration" at start but never says the tenant is authority-bearing | Unstated | **No** (SEC-14) |
| Actor (rate limiter, tenant configuration, corpus statistics) | Unstated | n/a | **No** (SEC-14) |
| Access-telemetry service | **Unstated** — AD-17 (`:152-156`) contains no authorization clause, and the Capability map (`:285`) governs access telemetry by AD-15/AD-17/AD-18 with **AD-5 absent** | Unstated | **No** (SEC-05) |
| Tenant creation | **Unstated** — by construction there is no pre-existing tenant claim to check; AD-6 (`:90`) describes the workflow shape, not who may start it | n/a | **No** (SEC-19) |

The spine's own guardrail — "Dapr channel authentication never replaces tenant authorization" (`:84`) — is a prohibition with no positive counterpart for the four unstated rows. Saying what may not authorize does not tell an implementer what does.

The case column is empty everywhere by design: AD-5's *Prevents* clause (`:83`) explicitly lists "case membership" among the things that may not serve as authorization, and no rule anywhere supplies an alternative. See SEC-08.

### 2. System-principal allowlist (AD-5) — **FAIL**

The allowlist is named once, in one sub-clause of `:84`: "one operator-owned finite allowlist to a canonical `system:*` principal and explicit tenant grant." Everything an operator or reviewer needs is absent: no owner role (contrast AD-17, which names **Platform Operations** for telemetry), no storage location, no change-control or review path, no integrity/authenticity requirement, no behavior on a miss, and no statement of grant cardinality. `prd.md:1143` (NFR10) *does* state "Unknown apps and ungranted tenants fail closed" — the architecture that binds NFR8–NFR11 is weaker than the requirement it binds. See SEC-07.

### 3. Case isolation (AD-7, AD-11, AD-9) — **FAIL**

The mechanical partitioning is genuinely good and the prior review's SP-H1 closure holds: AD-7 (`:96`) separates authoritative case ownership from query scope, AD-9 (`:108`) partitions tenant-wide graph seeds by ascending ordinal case ID and retains each unit's maximum graph score *within its case*, and AD-11 (`:120`) independently enforces case scope over every node and edge. A tenant-wide traversal cannot construct a cross-case path.

What fails is the premise. AD-5 removes case membership as an authorization input and nothing restores it, so within one tenant there is no case a caller "cannot access" — yet AD-7's heading says *Enforce case ownership*, AD-13 (`:132`) says "authorize before explaining evidence" without naming the subject of that authorization, and `prd.md` sells "team-scoped cases" (`prd.md:48`, `prd.md:56`) and gate G4 as case *isolation*. Two compliant implementations diverge on whether a tenant principal may read every case; the one that says yes satisfies every rule in the spine. Layered on top: AD-12 (`:126`) makes "omitted-detail handles" mandatory in every evidence-bearing result with no rule constraining what a handle may reveal or who may receive one (SEC-12), and the Errors convention (`:178`) forbids "implementation detail" in errors but never requires not-found and not-authorized to be indistinguishable (SEC-13).

### 4. Ordinal comparison as a security control (AD-5, Conventions) — **FAIL**

`StringComparison.Ordinal` with no case folding and no post-issuance normalization is the correct *comparison* rule and it is stated three times (`:84`, `:78`, `:175`). The spine constrains comparison and never constrains **issuance**. "Treat issued tenant and case IDs as validated opaque tokens" (`:84`) leaves "validated" undefined — no character set, no length bound, no canonical Unicode form, no reserved-character list.

Ordinal comparison is only a security control while the identifier stays inside .NET. It travels through at least seven systems that do not do .NET string comparison, and the spine mandates that travel:

- **Redis ACL principals and key permissions** — AD-6 (`:90`) requires "a per-tenant ACL principal resolved server-side". Redis expresses key permission as a **glob pattern**; `*`, `?`, `[`, `]` in a tenant ID become wildcards.
- **FalkorDB database/graph selection** (`:90`) — a graph name is a Redis key.
- **Dapr state keys and the preflight dedup key** — the Registry row (`:192`) says only "Key includes tenant/case and canonical request identity", with no unambiguous-composition rule; Dapr itself composes keys with `||`.
- **RediSearch index names**, **Dapr component scopes and topic names**, **URL path/query segments** (percent-encoding and proxy path normalization happen outside .NET), **JWT claim values**, **CloudEvent `source`/`id`** (`:78`), and **log/metric labels** (`:269`, "approved low-cardinality tenant representations", with no injectivity rule — SEC-17).

Where any of these systems folds case, normalizes Unicode, or interprets a metacharacter, the spine's ordinal rule is inert. See SEC-01.

### 5. Secrets (AD-15) — **PASS-WITH-FINDINGS**

Scope separation (bootstrap / application / data-plane / operator, `:144`), "secret references only" in workflow history (`:78`, `:143`, `:179`), no direct OpenBao client in application code, and execution-time secret resolution in activities (`:138`) are all stated and coherent. Two holes: the admitted Kubernetes-secret injection gap is declared "temporary" with **no owner, no expiry, and no production gate** (SEC-10) — unlike the OpenBao version gap, which the Stack note (`:214`) *does* bind to production qualification — and rotation appears in AD-15's *Binds* line (`:142`) but never in its Rule, so no compliant implementation is obliged to revoke a superseded credential (SEC-15).

### 6. Erasure (AD-16, AD-17) — **FAIL**

AD-16 (`:146-150`) is a real contract and it closed the prior review's C1. Walked adversarially it enumerates exactly three erasure targets — product projections purged, telemetry erasure mapping recorded, EventStore tenant-key crypto-shredding — plus four qualifiers (content-free tombstones, replay rejection, tenant-ID non-reuse, restore quarantine). Against the stores the spine itself names elsewhere:

| Store the spine names | Can hold tenant content or content-derived material | Covered by AD-16? |
| --- | --- | --- |
| Redis syntactic index (`:249`) | Yes — inverted index terms are source tokens | Yes, "product projections" |
| Redis vector index (`:250`) | Yes — embeddings are invertible toward source text | Yes, "product projections" |
| FalkorDB node/edge properties (`:251`) | Yes | Yes, "product projections" |
| EventStore payloads (`:148`) | Yes | Yes, crypto-shredding |
| **Dapr workflow history / Durable Task state** (`:74-78`, Deferred `:316`) | **Yes — ingestion activity inputs/outputs carry extracted text and vectors** | **No** (SEC-03) |
| **Actor state** (`:78`, `:180`) | Yes | **No** (SEC-03) |
| **Redis/FalkorDB backups** (Binds `:148`, Ops `:270`) | Yes — plaintext projections | **No** (SEC-02) |
| **FR71 application export bundles** (`prd.md:872`, `prd.md:903`, shipped Story 8.3) | Yes — portable JSON, plaintext | **No** (SEC-02) |
| Access telemetry (`:150`, `:156`) | **Asserted** opaque; never *required* to be | Deliberately excluded (SEC-06) |

The restore clause is the load-bearing defect. "Quarantine unreadable payloads during restore" (`:150`) triggers on *payload readability*, which is a proxy for "was crypto-shredded". A projection backup or an export bundle taken before erasure is perfectly readable, so the quarantine can never fire on it, and `prd.md:1159` (NFR16) explicitly requires that "export restore, and backup restore must never resurrect content from a tenant whose erasure completed under FR39." Operational Boundaries (`:270`) asserts AD-16 covers "backup admission" and "completion evidence"; neither term appears in AD-16's Rule.

Two further structural gaps: the erased-tenant register that enforces replay rejection and ID non-reuse has no stated home, durability posture, or precedence over a restored store (SEC-09), and the erasure mapping record — which by construction re-identifies "opaque" telemetry — has no stated reader set, no TTL of its own, and no statement of whether it is erasable (SEC-06, SEC-09).

### 7. Data integrity / state ownership (AD-2, AD-3, AD-4, AD-8, AD-14) — **PASS-WITH-FINDINGS**

Strong. EventStore as domain truth (`:66`), one Dapr-state projection coordinator with monotonic CAS and stale-ack rejection (`:72`), actor/workflow state as coordination not truth (`:180`), and a finite Direct Redis Exception Registry with an explicit "any addition requires a new architecture decision" (`:102`, `:194`) give most state a single named owner, and the five unregistered direct-Redis coordination uses are honestly recorded as a gap (`:300`) rather than tolerated. Residual: the embedding-configuration epoch — which gates AD-3 completion (`:72`) and AD-14 activation (`:138`) — is assigned to a "Tenant configuration actor" by the Capability map (`:282`) while AD-4 (`:180`) says actor state is not truth (SEC-18); and the erased-tenant register and the system-principal allowlist are two pieces of security-critical state with no owner at all (SEC-07, SEC-09).

### 8. Compliance posture — **FAIL**

`prd.md:546` promises a tenant that deletion makes content "irreversibly inaccessible with verification" and that "replay, restart, and restore cannot resurrect the content", and `prd.md:551` commits to a guide "showing how tenant delete maps to erasure". The architecture cannot evidence that promise:

- It does not enumerate the stores erasure must cover (Area 6), so a completion claim is unfalsifiable.
- "with verification" (`:150`) never says what verification demonstrates — key destruction, ciphertext unreadability, projection emptiness, or all three.
- AD-17 (`:156`) requires that the platform "never certify a tamper-evident audit trail", and access telemetry is the only durable per-tenant record of who did what. Erasure completion evidence therefore rests on a surface the architecture explicitly declines to certify, with no alternative integrity-protected evidence store named.

This is an architecture finding, not a legal opinion: the gap is between the *evidence the architecture produces* and the *evidence the stated product promise requires*. The PRD is scrupulous about never calling telemetry an audit trail (`prd.md:96`, `prd.md:548`); the erasure promise is where the two collide. See SEC-11.

---

## Findings

Severity key: **critical** = defeats a stated MVP hard gate (NFR8/G2, FR39/NFR16) as written; **high** = a compliant implementation can produce cross-tenant disclosure, content survival, or an unauthorized write; **medium** = information disclosure, integrity ambiguity, or an ungoverned control; **low** = evidence or attribution weakness.

---

### SEC-01 — critical — No issuance-time grammar for tenant/case identifiers makes the ordinal-comparison control inert outside .NET

**Spine:** `:84` (AD-5 Rule, "validated opaque tokens"), `:90` (AD-6, per-tenant Redis ACL principal / separate Falkor graph), `:175` (Conventions, Identifiers), `:192` (Registry, "Key includes tenant/case and canonical request identity").

**A compliant implementation permits this — explicitly.** Nothing in the spine constrains what an issued identifier may *contain*; it constrains only how one is compared. Two implementations both obey `:84`: one issues `^[a-z0-9][a-z0-9-]{2,62}$`, the other accepts any validated non-empty string.

**Scenario.** An operator (or the tenant-provisioning automation, whose own authority is unstated — SEC-19) creates a tenant with ID `a*`. AD-6 requires a per-tenant Redis ACL principal resolved server-side; the implementation composes that principal's key permission from the tenant ID, yielding a pattern such as `~a*:*`. Redis evaluates ACL key permissions as globs, so this principal reads and writes **every key belonging to every tenant whose ID begins with `a`** — including `acme`'s syntactic index, vector index, and dedup keys. The isolation boundary the addendum calls primary ("per-tenant Redis ACL users", `addendum.md:49`) is bypassed without a single rule being broken. G2/NFR8 ("zero cross-tenant leaks", `prd.md:1141`) fails.

Two collateral variants of the same missing rule: (a) with no unambiguous key-composition rule, tenant `a` + case `b:c` and tenant `a:b` + case `c` produce the same composed key, so one tenant's preflight reservation (`:192`) and — more seriously — one tenant's AD-3 projection checkpoint (`:72`) can be consumed by another tenant's acknowledgement; (b) Kubernetes object names are DNS-1123 lowercase, so if a deployment derives any per-tenant object name from the tenant ID, tenants `Acme` and `acme` — ordinal-distinct and therefore two tenants to the spine — collapse to one credential outside .NET.

**Closing text (AD-5 Rule).** After "Treat issued tenant and case IDs as validated opaque tokens", insert an issuance rule: *"Issuance restricts tenant, case, and MemoryUnit IDs to a single documented ASCII grammar with a bounded length, excluding every character that is a metacharacter, delimiter, or wildcard in Redis ACL key patterns, Redis/RediSearch/FalkorDB key and index names, Dapr key and component names, URL path segments, or Kubernetes object names. Any composed key, ACL pattern, index name, or state key built from these IDs uses an unambiguous composition (length-prefixed or with an escaped delimiter reserved out of the grammar). Comparison remains ordinal; issuance, not comparison, is what makes ordinal comparison sufficient."*

---

### SEC-02 — critical — Erasure quarantine keys on payload readability, so readable pre-erasure backups and export bundles restore intact

**Spine:** `:148-150` (AD-16 Binds "backups/restores"; Rule "quarantine unreadable payloads during restore rather than rehydrating them"), `:270` (Ops asserts "backup admission"). **Requirement:** `prd.md:1159` (NFR16), `prd.md:546`, FR71 (`prd.md:872`, `prd.md:903` — shipped).

**A compliant implementation permits this.** The quarantine predicate is a property of the *payload* ("unreadable"), which is true only of EventStore records whose tenant key was destroyed. Nothing in AD-16 makes the predicate a property of the *tenant*.

**Scenario.** Tenant `claims` is erased on day 1: projections purged, EventStore tenant key destroyed, verification recorded. On day 30 an operator restores the Redis and FalkorDB volumes from a day-0 snapshot after a cluster incident. Every payload in that snapshot is plaintext, so the quarantine rule never fires; the full syntactic index, the vector index (embeddings invertible toward the source text), and the FalkorDB graph for `claims` rehydrate. Search over `claims` works again. Separately, a day-0 FR71 `memories export tenant --tenant claims` JSON bundle sitting in object storage or a support ticket is plaintext by design, is covered by no rule in the spine, and re-importing it — under the same ID (rejected by non-reuse) *or under a new tenant ID* (rejected by nothing) — restores the erased content wholesale. NFR16's named requirement that "export restore, and backup restore must never resurrect content from a tenant whose erasure completed" cannot be met by the architecture as written.

**Closing text (AD-16 Rule).** Replace "quarantine unreadable payloads during restore rather than rehydrating them" with: *"Every restore path — EventStore restore, projection-store restore, and application export re-import — consults a durable erased-tenant register before admission and refuses to rehydrate any record whose tenant appears there, regardless of whether the payload is readable. Application export bundles (FR71) are erasure-scoped artifacts: each is registered at creation with its tenant and issuance time, and tenant erasure invalidates every registered bundle for that tenant, or bundles are wrapped under the tenant key so shredding covers them. Restore refuses to proceed when the register is older than the data being restored."* Add "application export bundles and projection-store backups" to AD-16's *Binds* line and define "backup admission" (`:270`) in the Rule rather than only in Operational Boundaries.

---

### SEC-03 — critical — Durable workflow and actor state carry tenant content and are named by no erasure target

**Spine:** `:74-78` (AD-4: *Prevents* lists only "secrets in workflow history"; Rule requires only that secrets be held as references), `:143` and `:179` (secret exclusion restated), `:150` (AD-16 targets: product projections, telemetry mapping, EventStore key), `:180` (actor/workflow state is "durable coordination"), `:316` (Deferred: the actor/workflow state store is the current Redis component).

**A compliant implementation permits this.** The spine excludes exactly one class of data from workflow history — secrets. Two implementations obey every rule: one passes only identifiers between ingestion activities and re-reads content from EventStore at each step; the other passes extracted text and embedding vectors as activity inputs and outputs, which the Durable Task runtime persists to workflow history by construction. Only the first survives erasure.

**Scenario.** Tenant `claims` ingests a 40-page settlement PDF. The extract activity returns the document text, the embed activity takes it as input and returns a 768-dimension vector; both values are written to workflow history in the Redis actor/workflow state store. Tenant `claims` is later erased: product projections are purged, the EventStore tenant key is destroyed, verification passes, the deletion is reported complete to the tenant. The extracted text and the vector remain in workflow history — they are not a product projection, they are not an EventStore payload, and crypto-shredding did not touch them because they were never encrypted under the tenant key. Anyone with operational read access to the state store (or a later backup of it — see SEC-02) reads the erased document. The same argument applies to any actor holding per-tenant material.

**Closing text (AD-16 Rule).** Extend the first clause to: *"Tenant deletion completes after every store that can hold tenant content or content-derived material is purged — product projections, durable workflow history and activity payloads, actor state, caches, and derived artifacts including embeddings and index terms — a durable access-telemetry erasure mapping/handoff is recorded, and the Hexalith.EventStore tenant-key crypto-shredding workflow…"*. Add the corresponding prohibition to AD-4's Rule: *"Workflow history and activity payloads carry identifiers, references, and non-secret configuration only; tenant content and content-derived material (extracted text, embeddings, snippets) are read from an erasure-scoped store at execution time and never persisted into workflow history."* That second sentence is the cheaper fix — it makes the erasure obligation structural instead of a purge chore.

---

### SEC-04 — high — The Dapr pub/sub ingress `/events/ingest` has no stated tenant-authority source

**Spine:** `:176` (the only mention of the route, in the Conventions table, as "the named infrastructure exception"), `:84` (AD-5 enumerates REST/CLI, MCP, and "trusted internal calls" — not pub/sub delivery; and *Prevents* at `:83` forbids "request fields" from serving as authorization), `:78` (AD-4 scopes CloudEvent identity by "tenant, case, exact validated `source`, and `id`" — identity, not authority), `:126` (AD-12 gates the *product capability* until L1–L3).

**A compliant implementation permits this.** AD-4 tells an implementer to read a tenant off the CloudEvent for identity purposes and AD-5 never says where a delivered event's tenant *authority* comes from, so taking the event's tenant field as authoritative violates no rule while contradicting AD-5's *Prevents* clause — the spine argues with itself and the Rule text wins.

**Scenario.** Phase 1.5 activates. Tenant `acme`'s application publishes domain CloudEvents to the shared Dapr topic; the sidecar delivers them to `/events/ingest`. A second subscriber application — or any workload that can publish to that pubsub component, including one whose app ID is on the allowlist for an unrelated purpose — publishes events with `tenant: acme` and attacker-authored payloads. They are accepted, indexed, embedded, and graph-linked into `acme`'s knowledge base, and thereafter surface in `acme`'s search results and Evidence Packets with system provenance. This is a cross-tenant *write*, and its downstream effect is worse than disclosure: an agent answering "what led to the API redesign?" is answered from injected content that carries the platform's own provenance markers. AD-12's L1–L3 gate does not close this — it gates product capability and public announcement, not the deployed endpoint's authorization model.

**Closing text (AD-5 Rule).** Add a third derivation sentence: *"Dapr pub/sub ingress at `/events/ingest` derives tenant authority from the delivery channel, not from the envelope: the receiving pubsub component or topic is bound to exactly one tenant, or the publishing app ID maps through the AD-5 allowlist to a `system:*` principal whose explicit tenant grant must contain the envelope's tenant. An envelope tenant that the channel does not authorize is rejected, not ingested. CloudEvent fields supply identity for AD-4 duplicate suppression and never supply authority."*

---

### SEC-05 — high — The access-telemetry service is a second per-tenant datastore with no authorization rule and no isolation mechanism

**Spine:** `:152-156` (AD-17's Rule names TTL, purge progress, erasure mapping, recovery, debt, activation, sanitization, and delivery bounds — and contains no authorization or tenant-scoping clause), `:285` (Capability map governs "Telemetry and operations" by **AD-15, AD-17, AD-18** — AD-5 and AD-6 are absent), `:90` (AD-6 supplies a per-tenant isolation mechanism for Redis and FalkorDB only), `:226-227` and `:259` (AccessTelemetry is an independently deployed service in AppHost and Kubernetes). **Context:** `addendum.md:71` records that access telemetry may use PostgreSQL — a store AD-6 never mentions.

**A compliant implementation permits this.** No rule requires the telemetry service to derive tenant authority, and the Capability map affirmatively omits AD-5 from its governance. An implementation that authorizes telemetry reads and one that does not both satisfy the spine.

**Scenario.** `memories status telemetry --tenant claims` is shipped (`prd.md:898`). The CLI calls the Server, which invokes the AccessTelemetry service over Dapr. The Server is on the allowlist, so the channel is authorized; AD-17 asks the telemetry service for nothing further, so it answers for whatever tenant the request names. A caller authenticated to tenant `pilot` whose request reaches the telemetry service with `tenant=claims` — through a Server bug, a direct in-cluster invocation from any allowlisted app, or the operator CLI — receives tenant `claims`'s search and access history: who searched, when, and what was attempted. Because the telemetry store is outside AD-6, it has no per-tenant principal, no tenant-scoped index, and no separate database; a single shared credential reads every tenant's records. NFR8's "a principal whose tenant claims name tenant A cannot read… tenant B's data" (`prd.md:1141`) is not scoped to product projections.

**Closing text.** Add AD-5 and AD-6 to the Capability map's telemetry row (`:285`), and add to AD-17's Rule: *"Access-telemetry reads and writes derive tenant authority under AD-5 like any other surface; the telemetry service authorizes independently of the calling workload's channel identity. The telemetry store is a tenant-isolated resource under AD-6 with a stated per-tenant principal or partition, provisioned and erased by the tenant lifecycle."*

---

### SEC-06 — high — "Retained opaque access telemetry" is an assertion in AD-16 that AD-17 never obliges

**Spine:** `:150` (AD-16 carves retained telemetry out of erasure on the strength of the word **opaque**), `:156` (AD-17 requires "sanitized product writes" — *sanitized* is never defined), `:155` (*Prevents* mentions "sensitive payload capture" but a Prevents line is a rationale, not a rule). **Requirement:** `prd.md:1202` (NFR34), FR67 (`prd.md:1094`: "records search and access events per tenant").

**A compliant implementation permits this.** AD-16's carve-out is legitimate only if the retained records are content-free, and no rule makes them so. One implementation stores a hashed query fingerprint; another stores the raw query string. Both are "sanitized" by their own definition, and both satisfy the spine.

**Scenario.** A user searches tenant `claims` for `"Henderson settlement amount above 2.4M"`. The access-telemetry record retains the query text. Tenant `claims` is erased; AD-16 explicitly declines to purge or accelerate purge of that record, which now survives for the configured TTL — potentially months. The record is also correlatable back to the erased tenant by the erasure mapping AD-16 requires (`:150`), which has no stated reader set, no TTL of its own, and no statement of whether it is itself erasable. The erasure promise at `prd.md:546` is broken by retained free-text content that the tenant's own searches produced, and the mapping guarantees it stays attributable.

**Closing text (AD-17 Rule).** Define the term the erasure carve-out depends on: *"Sanitized means content-free: access-telemetry records carry tenant, case, principal, operation, outcome, and timing as opaque or enumerated values, and never carry query text, snippets, extracted content, memory-unit content fields, or any value derived from them. A record that cannot be written content-free is not written."* In AD-16, state the erasure mapping's home, its reader set, and its own retention bound, and note that it expires no later than the telemetry it maps.

---

### SEC-07 — high — The system-principal allowlist has no owner, store, change control, miss behavior, or grant cardinality

**Spine:** `:84` (the allowlist's only appearance). **Requirement:** `prd.md:1143` (NFR10: "Unknown apps and ungranted tenants fail closed"), `addendum.md:63`.

**A compliant implementation permits this.** AD-5 says the allowlist is "operator-owned" and "finite" and stops. An implementation that reads it from a ConfigMap that any deployment pipeline can patch, that grants `system:indexer` every tenant with a wildcard, and that falls back to a default principal on a miss violates nothing in the spine — while NFR10 forbids the last of those.

**Scenario, three ways.** (a) *Miss behavior:* an app ID absent from the allowlist arrives on an mTLS-authorized channel; AD-5 says what to do on a hit and nothing on a miss, so an implementation grants a default `system:*` principal and the allowlist stops being a control. (b) *Grant cardinality:* AD-3's projection coordinator, AD-14's migration workflows, and AD-18's repair work all legitimately span tenants, so the practical grant for those principals is "all tenants"; nothing in `:84` forbids a wildcard, so "explicit tenant grant" degrades to `*` and the internal path has no tenant boundary at all. (c) *Integrity:* the allowlist is authorization data, not a secret, so AD-15 does not cover it and the Configuration convention (`:179`) governs only secret references — anyone who can edit ordinary configuration can add an app ID with a grant for any tenant, and no rule requires that change to be reviewed, signed, or observable.

**Closing text (AD-5 Rule).** Extend the allowlist clause: *"The allowlist is owned by Platform Operations, held in the operator secret scope under AD-15 rather than in ordinary configuration, versioned, and changed only through a reviewed and observable operator procedure. An app ID absent from the allowlist, or a tenant absent from that principal's grant, fails closed with no default principal and no fallback. Tenant grants are enumerated; a wildcard grant is a separately adopted architecture decision naming the principal, its purpose, and its evidence."*

---

### SEC-08 — high — No rule derives a caller's authority over a case, while three ADs read as though one exists

**Spine:** `:83` (AD-5 *Prevents* lists "case membership" as something that may not authorize), `:84` (AD-5 Rule derives tenant and subject authority; case is absent), `:96` (AD-7 "Resolve authoritative case ownership server-side" — which case owns a *unit*, not which caller may reach a *case*), `:132` (AD-13 "authorize before explaining evidence" — subject and granularity unstated), `:94` (AD-7 *Binds* NFR8 and G4). **Requirement:** `prd.md:187` (G4 "Case ownership/isolation tests"), `prd.md:48`/`:56` (team-scoped cases), `addendum.md:63` ("Tenant claims authorize. Case membership is metadata.").

**A compliant implementation permits this.** The addendum resolves the ambiguity — case is metadata, not authority — but the addendum is not the architecture, and the spine's own headings point the other way. One implementation lets any tenant principal read any case; another enforces per-case membership. Both satisfy every rule in the spine, and only one matches the "team-scoped cases" the PRD sells.

**Scenario.** Tenant `bu-legal` holds cases `hr-investigations` and `general`. A principal whose work is confined to `general` issues a tenant-wide search (a scope AD-7 explicitly permits). The results legitimately include units from `hr-investigations` with mandatory case attribution (`:96`), each carrying source/origin, confidence, and per-axis contribution (`:126`). Nothing was violated. If any deployment treats cases as a confidentiality partition — and the PRD's own journeys encourage that reading — this is a disclosure that the architecture neither prevents nor warns about. The reverse divergence is equally live: an implementation that adds per-case membership checks silently breaks FR34 tenant-wide discovery for callers who belong to one case.

**Closing text (AD-5 Rule or a new sentence in AD-7).** State the position once, either way. If case is not an authorization boundary in MVP: *"Case is a data-partition and attribution boundary, not an authorization boundary: a principal with tenant authority may read every case in that tenant, and case-scoped queries constrain results, not access. Per-case authorization is deferred and, until adopted, product documentation must not present cases as an access-control mechanism."* If it is one, AD-5 must derive case authority for every surface in the Area 1 table. Either sentence closes it; silence does not.

---

### SEC-09 — high — The erased-tenant register that enforces replay rejection and ID non-reuse has no home, durability posture, or restore precedence

**Spine:** `:150` (AD-16 "reject replay and reuse of the tenant ID" — no store named), `:150` ("Retain only content-free deletion evidence/tombstones"), `:316` (Deferred: the actor/workflow state store is the current Redis component, unqualified for durability).

**A compliant implementation permits this.** AD-16 states an obligation and names no owner for the state that discharges it. AD-2 (`:66`) makes EventStore domain truth, which suggests a tombstone; but AD-16 also destroys that tenant's key, and nothing says the tombstone is written outside the shredded keyspace.

**Scenario, two ways.** (a) *Self-shredding enforcement record:* the implementation records the deletion tombstone in the tenant's own EventStore stream and then destroys the tenant key. The tombstone is now unreadable. The non-reuse check queries it, finds nothing legible, and permits tenant ID `claims` to be re-created — at which point the new tenant inherits any surviving key prefix, ACL principal name, graph name, and dedup key of the erased one. (b) *Restorable enforcement record:* the register lives in Dapr state on the Redis component that Deferred `:316` says is not yet qualified for durability. Redis is restored from a pre-erasure snapshot after an incident; the register loses the erasure record, replay rejection stops rejecting, and SEC-02's restore path is no longer even nominally guarded.

**Closing text (AD-16 Rule).** *"A durable erased-tenant register, held outside any tenant-scoped keyspace and not encrypted under a destroyed tenant key, records each completed erasure with its tenant ID and completion evidence. It is the single authority for replay rejection, tenant-ID non-reuse, and restore admission, is retained indefinitely as content-free evidence, and its store has a stated durability and restore posture: any restore that would return the register to a state older than the data being admitted fails closed."*

---

### SEC-10 — high — The Kubernetes-secret gap is unbounded, and the gap table understates it: it removes the only stated mechanism for the NFR8 hard gate

**Spine:** `:144` (AD-15: "Direct Kubernetes-secret injection of Redis/FalkorDB credentials is a temporary alignment gap, not a second approved application secret path" — no owner, no expiry, no gate), gap row `:298` ("Shared Redis/FalkorDB credentials are injected through Kubernetes Secrets; no tenant backend principals exist" / "Violated rule: AD-6, AD-15"), `:214` (by contrast, the OpenBao version gap *is* bound: "Production qualification is blocked until…"), `:271` (Deployment boundary blocks Production on the EventStore gateway but not on this), `:300` (five direct-Redis coordination uses outside the exception registry).

**This is the one place where the spine's own gap table understates a security consequence** — the condition the review prompt reserves for an implementation-level finding.

**Scenario.** Row `:298` is classified as an ordinary convergence item, listed between "Fusion does not perform AD-9's canonical preprocessing" and "AppHost uses floating images", and its "Required convergence" column asks for provisioning work with no gate attached. What the row does not say is what is true while it stands: there is **one** shared Redis credential with access to every tenant's keyspace, so `addendum.md:49`'s stated primary boundary ("per-tenant Redis ACL users") does not exist and the actual boundary is application-layer key-prefix filtering — which the same addendum line explicitly says is "a placement tool, not the primary security boundary". Gap row `:300` compounds it: five coordination paths (permanent dedup, failed-unit registries, import leases, derived-store fences, migration state) reach Redis directly, outside the named adapters where AD-6's server-side principal resolution would be applied, each one a place where a missing prefix check is a full cross-tenant read. G2/NFR8 is an MVP *hard gate* (`prd.md:185`, `prd.md:1141`), and the architecture currently gates Production on an OpenBao patch level but not on the absence of the isolation mechanism that gate names.

**Closing text.** In AD-15's Rule, bind the exception the way the OpenBao exception is bound: *"…is a temporary alignment gap with a named owner and a dated expiry recorded in the Stack note; Production qualification is blocked until per-tenant backend principals replace it, or a dated, time-bounded security exception is approved."* In the gap table, restate row `:298`'s consequence: *"Until convergence, no per-tenant backend principal exists; tenant isolation rests entirely on application-layer scoping, including the five unregistered direct-Redis coordination paths in the row below, and G2/NFR8 cannot be evidenced at the boundary the requirement names."*

---

### SEC-11 — high — Erasure "verification" and "completion evidence" are asserted but undefined, and the only durable evidence surface is one the architecture refuses to certify

**Spine:** `:150` ("irreversibly invalidates or deletes content access **with verification**" — what verification proves is unstated), `:270` (Ops claims AD-16 joins "…and completion evidence into one outcome"; the term appears in no AD Rule), `:156` (AD-17: telemetry must "never certify a tamper-evident audit trail"). **Requirement:** `prd.md:546`, `prd.md:551` (compliance guide mapping tenant delete to erasure), `prd.md:1052` (FR39 "verified tenant erasure").

**A compliant implementation permits this.** "With verification" admits any check an implementer chooses: that the key-destruction API returned 200, that a sample ciphertext no longer decrypts, that the projection indexes are empty, or nothing beyond a log line. All satisfy `:150`.

**Scenario.** A tenant exercises an erasure right and asks the operator to evidence completion. The operator can produce: a content-free tombstone whose integrity posture the spine never states; access-telemetry records that AD-17 forbids presenting as a tamper-evident trail; and a workflow completion status in a state store that Deferred `:316` says is not yet qualified for durability. Nothing enumerates *which* stores were purged (SEC-02, SEC-03), so no evidence artifact can be complete even in principle. The product promise at `prd.md:546` — "irreversibly inaccessible **with verification**", mapped to erasure in a published compliance guide — is stronger than what the architecture can evidence.

**Closing text (AD-16 Rule).** *"Verification means a recorded, reproducible check per erasure target: for each enumerated store, evidence that tenant content is absent or unreadable, and for the EventStore tenant key, evidence that the key material is destroyed and that a sampled ciphertext no longer decrypts. Completion evidence is a content-free record written to the erased-tenant register with the target list, per-target verification outcome, and completion time; it is domain evidence under AD-2, not access telemetry, and AD-17's non-certification applies to telemetry only."*

---

### SEC-12 — medium — Mandatory "omitted-detail handles" are an existence oracle with no authorization rule

**Spine:** `:126` (AD-12: every evidence-bearing result carries "omitted-detail handles"), `:132` (AD-13 "authorize before explaining evidence" — no rule covers *emitting* a handle), `:96`, `:114`.

**A compliant implementation permits this.** AD-12 makes handles mandatory; no rule says a handle may be withheld, must be non-enumerable, or must be scoped to the caller. A handle emitted *because* detail was withheld for authorization reasons is the one case where the handle itself is the disclosure, and the spine does not distinguish it from a handle emitted for size or degradation reasons.

**Scenario.** A search returns a handle for a result whose detail was omitted. If the handle is a MemoryUnitId or a case-qualified reference, its presence confirms that a matching unit exists in a scope the caller could not see, and its value can be replayed against other surfaces (REST, MCP, export) or correlated across queries to map another case's or another tenant's corpus by existence alone. Because the same envelope also carries per-axis contribution and confidence (`:126`), a caller can additionally learn *how well* the hidden unit matched.

**Closing text (AD-12 Rule).** After "omitted-detail handles", add: *"A handle is emitted only for detail the caller is authorized to retrieve; detail withheld for authorization reasons is omitted with no handle and is indistinguishable from absence. Handles are opaque, non-enumerable, scoped to the issuing caller and request, and carry no score, count, or scope information about unauthorized content."*

---

### SEC-13 — medium — No rule requires not-found and not-authorized responses to be indistinguishable

**Spine:** `:178` (Errors convention: "stable code, human message, and actionable suggestion **without implementation detail**"), `:96` (AD-7: mutations, annotations, and deletion "verify the unit's owner case" — the failure response is unspecified), `:132`.

**A compliant implementation permits this.** "Without implementation detail" excludes stack traces and connection strings; it says nothing about existence disclosure, and an "actionable suggestion" pushes implementations toward *more* specific messages, not fewer.

**Scenario.** A caller with tenant authority for `bu-legal` issues `DELETE /api/v1/.../memory-units/{id}` (or an annotation, per FR37) against an ID observed in an Evidence Packet, an export bundle, or a log. A compliant implementation replies "unit belongs to case `hr-investigations`" with the actionable suggestion "request access to that case" — confirming the unit's existence, its owner case, and by repetition the shape of another case's corpus. The same divergence appears across tenants: a distinct "not your tenant" versus "no such unit" reply turns opaque MemoryUnitIds into a probe oracle, which is precisely the property AD-13 (`:132`) tries to protect by making the ID opaque.

**Closing text (Errors convention row).** Append: *"Authorization and existence failures on scoped resources return one indistinguishable response — the same code, message, and timing class — whether the resource does not exist or the caller may not reach it. Errors never name a scope, case, tenant, or resource the caller is not authorized to see; the actionable suggestion is generic in that case."*

---

### SEC-14 — medium — Background, repair, replay, and migration work has no stated tenant-authority source

**Spine:** `:72` (AD-3: repair and replay use the same protocol), `:78` (AD-4: workflows capture "immutable non-secret configuration plus its epoch at start" — configuration, not authority), `:138` (AD-14 migrations), `:162` (AD-18 repair/migration), `:84` (AD-5 covers callers, not resumed durable state). **Requirement:** `prd.md:1107` (FR74: repair "cannot cross tenants").

**A compliant implementation permits this.** AD-5 derives authority at ingress. A workflow that resumes after a restart, a reminder-driven actor, or an operator-scheduled repair sweep has no ingress to derive from, and no rule says the tenant captured at start is authority-bearing, revalidated on resume, or checked against tenant state.

**Scenario.** A repair or replay sweep is scheduled for tenant `acme` and its workflow is durable. Between scheduling and execution, tenant `acme` is deleted (AD-16) or deactivated (AD-6 requires "an active verified tenant" for "other paths", but does not say a resumed workflow is such a path). The activity resolves its secret reference at execution time (`:138`), obtains the shared data-plane credential (SEC-10), and writes projections for a tenant that no longer exists — re-populating indexes for an erased tenant, or, if the sweep enumerates tenants from a stale captured configuration, touching a tenant it was never granted. FR74's "cannot cross tenants" has no architectural anchor.

**Closing text (AD-5 Rule).** *"Work with no human caller — workflows, activities, actors, reminders, repair, replay, and migration — carries an explicit tenant authority captured at initiation from an AD-5 derivation, revalidates it against active tenant state on every resume and at each activity boundary, and fails closed when the tenant is inactive, erased, or absent from the initiating principal's grant. Configuration captured at start is not authority."*

---

### SEC-15 — medium — Rotation appears only in AD-15's Binds line; no rule obliges revocation of a superseded credential

**Spine:** `:142` (AD-15 *Binds* "rotation"), `:144` (Rule — rotation absent), `:138` (AD-14: "activities resolve secret references at execution time so rotation can retry safely" — the only rotation obligation in the spine, and it is about retry safety).

**A compliant implementation permits this.** AD-14 makes rotation *survivable*; nothing makes it *effective*. An implementation that issues a new credential and leaves the old one valid indefinitely satisfies both ADs.

**Scenario.** A per-tenant Redis ACL password is rotated after a suspected exposure. AD-14 guarantees in-flight activities re-resolve and retry; no rule requires the previous password to be revoked, so the exposed credential keeps working. Second variant: AD-14's execution-time resolution binds the *resolution*, not the resulting connection — a long-running activity or a pooled connection established before rotation continues to hold data-plane access after revocation, and no rule bounds the lifetime of a credentialed connection against the credential's own lifetime.

**Closing text (AD-15 Rule).** *"Rotation supersedes: a rotated credential is revoked at the provider within a stated bound, and no connection or client established under a superseded credential outlives that bound. Rotation of a tenant data-plane credential is a tenant-lifecycle operation under AD-6 with verification that the prior credential no longer authenticates."*

---

### SEC-16 — medium — The L1–L3 gate on MCP disables publication, not in-cluster reachability

**Spine:** `:126` (AD-12: MCP and CloudEvent product capabilities "remain inactive until Phase 1.5 L1-L3 gates pass, even if deployment assets exist"), gap row `:308` (Required convergence: "Keep public ingress/publication/announcement disabled until L1-L3 pass"), `:229` ("asset presence is not activation"), `:235` and `:259` (Kubernetes contains "gated MCP"; internal traffic uses the Dapr workload policy and the allowlist).

**A compliant implementation permits this.** The convergence text names three things to disable — public ingress, publication, announcement — and a deployed, in-cluster, Dapr-reachable MCP workload is none of them. "Gated" is asserted in the topology text without a rule defining the gate's enforcement point.

**Scenario.** The MCP workload runs in Production with public ingress off. Any app ID on the AD-5 allowlist — whose governance is itself unbound (SEC-07) — invokes it over Dapr service invocation. It serves a surface whose authorization evidence L1–L3 has not yet qualified, backed by the same shared data-plane credential (SEC-10). "Inactive" as a product state is not "unreachable" as a security state.

**Closing text (gap row `:308` and AD-12 Rule).** *"Until L1–L3 pass, gated capabilities are not reachable: the workload is not deployed, or it is deployed with the Dapr deny-by-default policy admitting no caller and its product routes returning a gated-capability error. Disabling public ingress alone does not satisfy the gate."*

---

### SEC-17 — low — "Approved low-cardinality tenant representations" has no injectivity or derivation rule

**Spine:** `:269` (Observability boundary), `:182` (structured logs without content or secrets), `:162` (AD-18 production evidence includes noisy-neighbor behavior), `:183` (integration evidence proves "authorization before data access").

**A compliant implementation permits this.** Low cardinality and injectivity are in direct tension and the spine picks neither. A truncated hash, a bucket index, or a reused label all satisfy `:269`.

**Scenario.** Two tenants map to the same metric and log label. Per-tenant index size, search latency, and queue depth (NFR29, `prd.md:1187`) merge; AD-18's noisy-neighbor evidence attributes one tenant's load to another; and an isolation incident cannot be attributed from telemetry because the label does not distinguish the tenants involved. This weakens isolation *evidence*, not isolation itself — hence low.

**Closing text (Observability row).** *"The approved tenant representation is a documented, stable, injective mapping from tenant ID to label, derived at issuance and recorded with the tenant; collisions are a provisioning error, not an accepted cardinality trade-off."*

---

### SEC-18 — low — The embedding-configuration epoch has two candidate owners

**Spine:** `:72` (AD-3: the epoch is part of the projection identity tuple and gates `Indexed`), `:138` (AD-14: activation is atomic and evidence carries the active epoch), `:282` (Capability map assigns "Provider and schema evolution" to a "Tenant configuration actor"), `:180` (Mutation and state: "actor/workflow state is durable coordination, not domain truth"), `:66` (AD-2: EventStore is domain mutation truth and binds tenant lifecycle).

**A compliant implementation permits this.** One implementation makes the epoch an EventStore-committed tenant-lifecycle fact; another holds it in actor state as the Capability map suggests, which AD-4 then says is not truth. Both are compliant, and only the first survives actor-state loss.

**Scenario.** Actor state is lost or restored stale during an AD-14 migration. The coordinator accepts acknowledgements carrying the old epoch, marks units `Indexed` under a superseded embedding configuration, and the tenant's corpus silently mixes embedding dimensions — the exact outcome AD-14's *Prevents* line (`:137`) names.

**Closing text (AD-14 Rule or the Capability map row).** *"The active schema generation and embedding-configuration epoch are tenant-lifecycle domain facts committed through AD-2 before any projection may cite them; the tenant configuration actor caches and serializes access to them and is never their source of truth."*

---

### SEC-19 — low — Tenant-creation authority is unstated

**Spine:** `:90` (AD-6 describes tenant lifecycle workflow shape and requires "an active verified tenant" for other paths), `:84` (AD-5 derives authority from a tenant claim — which cannot exist before the tenant does), `:278` (Capability map binds tenant lifecycle to AD-5 without AD-5 covering the creation case).

**A compliant implementation permits this.** The one authorization primitive AD-5 offers is a tenant claim, and creation is definitionally the operation with no such claim. Every implementation must invent something; nothing says what.

**Scenario.** Whoever can reach the tenant-creation path chooses the new tenant's ID, which under SEC-01 is the precondition for the Redis-ACL-glob cross-tenant read, and chooses whether it is created at all — the resource-exhaustion and squatting variants follow. This is low on its own and is the enabling step for SEC-01.

**Closing text (AD-5 Rule).** *"Tenant creation, deletion, and lifecycle repair are authorized by an operator principal from the AD-5 allowlist or an equivalently derived operator authority, never by a tenant claim; the authorizing principal is recorded in the lifecycle evidence."*

---

## Verified clean (checked, no finding)

- **Bearer authority across REST/CLI/MCP hops** (`:84`, `:266`): the prior review's H5 closure holds. The three layers — OIDC/JWT identity, Dapr mTLS/access-control workload authorization, app-token channel protection — are distinct, and "none substitutes for tenant/case checks" is stated twice.
- **Graph scope enforcement** (`:96`, `:108`, `:120`): tenant *and* case scope over every node and edge, case-partitioned tenant-wide seeding, deterministic per-case merge, forbidden cross-case paths, parameterized values, enum-restricted labels, server-owned depth/result/time limits, and a kill switch. SP-H1's closure survives adversarial reading.
- **CloudEvent identity scope** (`:78`): tenant + case + exact validated `source` + `id` with ordinal comparison closes SP-H2. Note this is *identity*, not *authority* — see SEC-04.
- **Telemetry/erasure lifecycle alignment** (`:150`, `:156`): AD-16 no longer forces early telemetry purge and matches AD-17 and the accepted lifecycle. SP-H3 is genuinely closed; the residual problem is the undefined meaning of "opaque"/"sanitized" (SEC-06), not the sequencing.
- **Direct Redis Exception Registry** (`:186-194`): one row, with primitive, failure posture, key scope, and mandatory test evidence; additions require a new `AD-n`; the five out-of-registry uses are recorded as a gap rather than tolerated.
- **Secrets in workflow history** (`:78`, `:143`, `:179`): stated three times, consistently. The gap is content, not secrets (SEC-03).
- **Provenance binding** (`:132`, gap row `:297`): caller-controlled `IngestedBy` is correctly identified as an AD-5/AD-13 violation with the right convergence.
- **Degradation honesty** (`:114`, `:110-114`): partial results must enumerate unavailable and excluded axes; degradation never promotes an unacknowledged revision to `Indexed`.

---

## Summary table

| ID | Severity | Area | One-line |
| --- | --- | --- | --- |
| SEC-01 | critical | 4 | No issuance grammar for IDs; Redis ACL globs and ambiguous key composition defeat NFR8 |
| SEC-02 | critical | 6 | Restore quarantine keys on payload readability; readable projection backups and FR71 exports rehydrate erased tenants |
| SEC-03 | critical | 6 | Workflow history and actor state hold extracted content and are named by no erasure target |
| SEC-04 | high | 1 | `/events/ingest` has no stated tenant-authority source; cross-tenant content injection |
| SEC-05 | high | 1 | Access telemetry has no authorization rule and is outside AD-5/AD-6 |
| SEC-06 | high | 6 | AD-16's "opaque" telemetry carve-out is an assertion AD-17 never obliges |
| SEC-07 | high | 2 | Allowlist: no owner, store, change control, miss behavior, or grant cardinality |
| SEC-08 | high | 3 | No rule derives case authority while three ADs read as though one exists |
| SEC-09 | high | 6 | Erased-tenant register has no home, durability, or restore precedence |
| SEC-10 | high | 5 | Kubernetes-secret gap is unbounded and the gap table understates its NFR8 consequence |
| SEC-11 | high | 8 | Erasure verification/completion evidence undefined; only durable evidence surface refuses certification |
| SEC-12 | medium | 3 | Mandatory omitted-detail handles are an unconstrained existence oracle |
| SEC-13 | medium | 3 | No not-found/not-authorized indistinguishability rule |
| SEC-14 | medium | 1 | Background/repair/replay/migration work has no tenant-authority source |
| SEC-15 | medium | 5 | No revocation obligation for a superseded credential |
| SEC-16 | medium | 1 | L1–L3 gate disables publication, not in-cluster reachability |
| SEC-17 | low | 4 | Low-cardinality tenant labels have no injectivity rule |
| SEC-18 | low | 7 | Embedding-configuration epoch has two candidate owners |
| SEC-19 | low | 1 | Tenant-creation authority unstated |

**Counts:** 3 critical, 8 high, 5 medium, 3 low (19 findings).

**Recommended disposition.** SEC-01, SEC-02, and SEC-03 are AD text changes, not new decisions, and each defeats a stated MVP hard gate as written — they should be applied before the spine is treated as final for security purposes. SEC-04 through SEC-11 each name a derivation, owner, or obligation the spine intends but does not state; all eight are additive sentences within existing ADs. SEC-10 is additionally a correction to the "Current Alignment Gaps" table's own characterization.
