# Reviewer Gate — Technology Currentness

**Target:** `_bmad-output/planning-artifacts/architecture.md`  
**Lens:** first configured Reviewer Gate lens — technology/version reality check  
**Review date / upstream access date:** 2026-09-09  
**Mode:** validation only; the target was not modified  
**Verdict:** **FAIL — not safe as the current implementation architecture**

The March 2026 architecture contains a still-recognizable technical direction, and most of the
technologies now used by the repository continue to exist and support .NET 10. The document is not,
however, a reliable current contract. It presents a dated package table and several superseded designs as
binding decisions, incorrectly classifies FalkorDB's server licence, names an embedding model that has
already been shut down, calls Dapr Agents GA contrary to Dapr's own status material, and omits current
security-patch drift in the deployed OpenBao and PostgreSQL images. Its own claims that all versions were
verified and that implementers should follow all 31 decisions exactly amplify those errors
(`architecture.md:1640-1643`, `1683-1688`, `1749-1752`, `1762-1765`).

## Review basis and interpretation

- The artifact was read in full (`architecture.md:1-1769`). Its frontmatter dates completion/revision to
  2026-03-24/25 (`architecture.md:1-16`), so a statement explicitly labelled “Current Verified Versions
  (March 2026)” is treated as historical evidence, not proof of 2026-09-09 currentness.
- Repository reality was taken from executable/configured sources: `global.json`, `Directory.Build.props`,
  the imported central package catalogue, project files, source, CI, and deployment manifests. Restored
  `obj/project.assets.json` graphs were inspected as a resolution cross-check.
- The change-controlled PRD and addendum were used only to distinguish approved September decisions from
  March residue. They explicitly say C# 14 and MIT and identify architecture drift
  (`prd.md:1-39`, `70-76`; `addendum.md:73-76`, `101-127`).
- “Exists” means an official release/package record remains available. “Fits” means the current pin has a
  plausible supported target/runtime relationship; it does not mean that unexecuted architecture samples
  were compiled or that production compatibility was proven.
- All external checks below use first-party project documentation, release repositories, or package
  registries. All URLs were accessed on 2026-09-09.

## Findings

### Critical

#### TC-01 — The licence decisions are factually wrong and the stated boundary does not mitigate them

**Classification:** genuine incompatibility / unsupported legal claim  
**Disposition:** **discuss** — correct the architecture immediately, then obtain an explicit dependency-
licence decision before treating FalkorDB as approved for distribution or hosted operation.

Evidence:

- The architecture labels FalkorDB “AGPL” (`architecture.md:65`, `201-209`) and says that risk is mitigated
  by the DAPR architectural boundary (`architecture.md:205-208`). The exact deployed server is
  `falkordb/falkordb:v4.12.0` (`deploy/kubernetes/base/falkordb-statefulset.yaml:32-40`). Its official
  v4.12.0 `LICENSE.txt` is the Server Side Public License v1, not AGPL. Process/network separation does not
  by itself reclassify or discharge licence obligations; the architecture supplies no legal source or
  analysis for that assertion.
- The expected root structure says `LICENSE` is Apache 2.0 (`architecture.md:1316-1329`), while the actual
  root file begins `MIT License` (`LICENSE:1-3`). The current PRD records that the 2026-09-08 MIT decision
  supersedes March Apache 2.0 (`prd.md:31-39`, `70-76`), and the addendum explicitly hands this drift back
  to architecture (`addendum.md:118-127`).
- Redis Stack is a useful counterexample: the architecture's `SSPL/RSAL` summary (`architecture.md:64`)
  is materially consistent with Redis's official licensing page for the deployed 7.4 line. The FalkorDB
  error is therefore not merely loose use of “source available.”

Primary sources (accessed 2026-09-09):

- FalkorDB v4.12.0 licence:
  https://github.com/FalkorDB/FalkorDB/blob/v4.12.0/LICENSE.txt
- FalkorDB official repository/licence declaration:
  https://github.com/FalkorDB/FalkorDB
- Redis licence matrix:
  https://redis.io/legal/licenses/

Acceptance condition: replace both licence labels with the exact current decisions, remove the unsupported
“DAPR boundary mitigates licensing” conclusion, identify which FalkorDB artefact is being evaluated
(server versus Apache-2.0 NFalkorDB client), and record the owner/date of the resulting legal/product
decision.

#### TC-02 — Deployed OpenBao and PostgreSQL pins predate security-fix patch releases

**Classification:** genuine current repository risk omitted from the architecture  
**Disposition:** **open** — assess and update the two image pins/digests, or record a time-bounded exception
with exposure analysis; then make the architecture name the operational version source of truth.

Evidence:

- OpenBao is pinned to `2.6.0` in CI, the AppHost development profile, Helm values, and the smoke test
  (`.github/workflows/ci.yml:14-20`; `src/Hexalith.Memories.AppHost/OpenBaoDevelopmentProfile.cs:14-18`;
  `deploy/openbao/values.yaml:24-28`; `deploy/openbao/smoke-test.yaml:28-34`). OpenBao 2.6.2 was released on
  2026-08-18 and its official notes include security fixes. The architecture's D31 describes the provider
  and evidence obligations but gives no pin, provenance, or patch review trigger
  (`architecture.md:650-677`).
- Access telemetry is pinned to PostgreSQL `18.4-trixie` (`deploy/kubernetes/base/access-telemetry-postgresql.yaml:120-145`).
  PostgreSQL 18.6 was released on 2026-08-13 specifically with fixes from 18.4, including multiple CVEs.
  The architecture mentions PostgreSQL only as the access-ledger implementation
  (`architecture.md:215-230`) and does not expose this version obligation.
- This finding does not claim that every listed CVE is reachable in Memories. It does establish that a
  “current technology” review cannot bless the older patch pins without an explicit exposure decision.

Primary sources (accessed 2026-09-09):

- OpenBao 2.6.x release notes:
  https://openbao.org/community/release-notes/2-6-0/
- PostgreSQL 18.6 release notes:
  https://www.postgresql.org/docs/release/18.6/
- OpenBao API compatibility warning (important for upgrade testing):
  https://openbao.org/api-docs/next/

Acceptance condition: disposition the security updates, refresh exact image digests where approved, run the
relevant component/integration evidence, and add a dated image-pin authority plus patch cadence to the
architecture.

### High

#### TC-03 — The normative dependency inventory is a March snapshot, not the repository's current graph

**Classification:** stale March 2026 narrative  
**Disposition:** **open** — regenerate the inventory from authoritative repo files and clearly separate
historical snapshots from current pins.

The architecture calls its table “Current Verified Versions (March 2026)” and pins Aspire 13.1.3,
CommunityToolkit 9.7.0, Dapr 1.17.6, NRedisStack 1.3.0, StackExchange.Redis 2.12.4, NFalkorDB 1.0.0,
xunit.v3 3.2.2, and NSubstitute 5.3.0 (`architecture.md:464-480`, `1250-1263`). It later says there are no
version conflicts and tells implementation to configure Dapr 1.17.6 (`architecture.md:1640-1643`,
`1762-1765`). Current authoritative pins are:

| Decision | Architecture | Current repository evidence | Currentness disposition |
|---|---|---|---|
| .NET / language | .NET 10 / C# 13 (`architecture.md:61`) | SDK 10.0.400 (`global.json:1-8`); `net10.0`, C# 14 (`Directory.Build.props:1-8`) | Supported, but language decision is superseded |
| Aspire | 13.1.3 | 13.5.3 (`references/Hexalith.Builds/Props/Directory.Packages.props:109-120`; AppHost SDK at `src/Hexalith.Memories.AppHost/Hexalith.Memories.AppHost.csproj:1`) | Supported; architecture stale |
| Aspire Dapr toolkit | 9.7.0 | `13.5.0-preview.1.260825-0345` (`.../Directory.Packages.props:134-137`; AppHost reference at `...AppHost.csproj:19-23`) | Exists, but prerelease risk is undisclosed |
| Dapr .NET packages | 1.17.6 | 1.18.5 (`.../Directory.Packages.props:139-146`) | Current stable SDK; architecture stale |
| Dapr runtime | not pinned in table | 1.18.2 in CI (`.github/workflows/ci.yml:14-17`) | Current supported 1.18 line; document the SDK/runtime split |
| Kreuzberg | 4.3.8 | 4.10.2 (`.../Directory.Packages.props:165-170`) | Exists; 4.10.3 is available, one patch newer |
| NRedisStack | 1.3.0 | 1.7.4 (`.../Directory.Packages.props:257-259`) | Current package; architecture stale |
| StackExchange.Redis | 2.12.4 | 3.1.31 (`.../Directory.Packages.props:257-259`) | Current package; architecture stale |
| NFalkorDB | 1.0.0 | 1.2.0 (`.../Directory.Packages.props:255-259`) | Current stable package; architecture stale |
| Test stack | xunit.v3 3.2.2 / NSubstitute 5.3.0 / Shouldly 4.3.0 | 4.0.0 / 6.2.0 / 4.3.0 (`.../Directory.Packages.props:294-321`) | Exists and fits; two architecture pins stale |

The restored Server, AppHost, and test asset graphs resolve these current catalogue values, so this is not
an unused-catalogue comparison. The server project directly references the stated Dapr, Kreuzberg,
NFalkorDB, NRedisStack, and StackExchange.Redis packages
(`src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj:43-59`).

Primary sources (accessed 2026-09-09):

- .NET 10 downloads/current SDK and C# 14:
  https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- .NET support policy:
  https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- Aspire.Hosting 13.5.3:
  https://www.nuget.org/packages/Aspire.Hosting/13.5.3
- CommunityToolkit Aspire Dapr versions:
  https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Dapr/
- Dapr .NET SDK releases:
  https://github.com/dapr/dotnet-sdk/releases
- Dapr support/release policy:
  https://docs.dapr.io/operations/support/support-release-policy/
- Dapr.Client package:
  https://www.nuget.org/packages/Dapr.Client/
- NRedisStack package:
  https://www.nuget.org/packages/NRedisStack/
- StackExchange.Redis package:
  https://www.nuget.org/packages/StackExchange.Redis/
- NFalkorDB registry versions:
  https://api.nuget.org/v3-flatcontainer/nfalkordb/index.json
- Kreuzberg registry versions:
  https://api.nuget.org/v3-flatcontainer/kreuzberg/index.json
- xunit.v3 package:
  https://www.nuget.org/packages/xunit.v3/
- NSubstitute package:
  https://www.nuget.org/packages/NSubstitute/
- Shouldly registry versions:
  https://api.nuget.org/v3-flatcontainer/shouldly/index.json

Acceptance condition: make `global.json`, imported central package management, CI variables, and deployment
manifests the explicit authorities; record preview packages and their graduation trigger; do not duplicate
volatile exact versions in a “current” prose table without an as-of date and update process.

#### TC-04 — D27/D28 bind implementation to a Python Dapr Agents service that is absent and not officially GA

**Classification:** unsupported maturity claim plus brownfield/code mismatch  
**Disposition:** **discuss** — decide whether the Python agent is still a future option or a required
component. Until implementation evidence exists, remove binding “must” language and mark the design as
deferred/assumed.

Evidence:

- The architecture calls Dapr Agents a “GA Python SDK,” pins 1.0.0, assigns enrichment to a Python service,
  and makes that topology mandatory (`architecture.md:61-69`, `256-270`, `336-344`, `596-605`,
  `1116-1232`, `1296-1300`, `1514-1533`).
- PyPI proves that the package exists and is now version 1.0.6; semantic version 1.0 alone is not GA
  evidence. Dapr's official SDK status page calls the Agents framework “In development,” while its own
  package metadata uses a pre-alpha development classifier.
- The actual repository has 15 C# projects, including AccessTelemetry, MCP, and EventStore projects, but
  no `services/ai-agent` product tree or product `pyproject.toml`. The implemented natural-language
  enrichment path registers `DaprConversationClient` and invokes it directly from a C# workflow activity
  (`src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs:122-127`;
  `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs:65-130`).
- The current addendum correctly calls the Python sidecar optional unless a later product requirement
  promotes it (`addendum.md:60-65`). That makes the architecture's mandatory boundary specifically stale,
  not merely incomplete code.

Primary sources (accessed 2026-09-09):

- Dapr SDK/framework status:
  https://docs.dapr.io/developing-applications/sdks/
- Dapr Agents PyPI release:
  https://pypi.org/project/dapr-agents/
- Dapr Agents official package metadata:
  https://github.com/dapr/dapr-agents/blob/main/pyproject.toml
- Dapr Agents releases:
  https://github.com/dapr/dapr-agents/releases

Acceptance condition: describe the C# Conversation client as the present brownfield implementation; place
Python/Dapr Agents in a dated future decision with a maturity gate, or add the missing deployable, pin,
tests, ownership, and supported Python range before restoring mandatory language.

#### TC-05 — The committed Google embedding model is retired; the repository already implements a different provider matrix

**Classification:** genuine upstream incompatibility plus stale provider decision  
**Disposition:** **open** — replace dead examples/migration paths and reconcile D4 with the implemented
Google/Ollama provider registry.

Evidence:

- The architecture uses `text-embedding-004` in the stable-ID example and describes migration to a
  hypothetical `text-embedding-005` (`architecture.md:104-118`, `286-301`). Google records
  `text-embedding-004` as shut down on 2026-01-14 and does not present `005` as its replacement.
- Current code uses `gemini-embedding-001` and also supports `qwen3-embedding:4b` through Ollama, with
  explicit dimension sets (`src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs:13-26`,
  `46-71`, `122-153`). Runtime endpoints for both are config sourced
  (`src/Hexalith.Memories.Server/appsettings.json:8-19`). Thus D4's “Google only for MVP” statement
  (`architecture.md:286-301`) no longer matches the implementation.
- Google's official model page still lists `gemini-embedding-001`, so the current Google code path names a
  real model. Google's current documentation identifies `gemini-embedding-2` as the newer general model;
  that does not automatically authorize a repo migration because dimensions/reindexing are architectural
  concerns.

Primary sources (accessed 2026-09-09):

- Gemini API deprecations/shutdown dates:
  https://ai.google.dev/gemini-api/docs/deprecations
- `gemini-embedding-001` model page:
  https://ai.google.dev/gemini-api/docs/models/gemini-embedding-001
- Current embeddings documentation:
  https://ai.google.dev/gemini-api/docs/embeddings

Acceptance condition: replace the dead model names, record the implemented provider/model/dimension matrix,
and preserve the existing reindex-on-breaking-change rule rather than inferring an automatic upgrade.

#### TC-06 — The architecture mandates MediatR/FluentValidation behavior that the Server does not use

**Classification:** brownfield/code mismatch  
**Disposition:** **discuss** — ratify the actual endpoint/domain validation design or separately authorize
and implement the pipeline; a central version entry alone is not adoption.

Evidence:

- The architecture says FluentValidation validators run via a MediatR pipeline and declares both mandatory
  (`architecture.md:818-826`, `1279-1287`).
- Current central management has MediatR 14.2.0 and FluentValidation 12.1.1 available
  (`references/Hexalith.Builds/Props/Directory.Packages.props:149-153`, `168-171`), but the Server's actual
  package references omit both (`src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj:43-59`), and
  no Server source usage of either namespace/type was found. Available is not the same as resolved or used.

Primary sources (accessed 2026-09-09):

- MediatR registry versions:
  https://api.nuget.org/v3-flatcontainer/mediatr/index.json
- FluentValidation registry versions:
  https://api.nuget.org/v3-flatcontainer/fluentvalidation/index.json

Acceptance condition: align the pattern section and enforcement table with compiled code, and cite the
owning project/package only if the pipeline remains an accepted decision.

### Medium

#### TC-07 — Development database images float while production images are digest-pinned

**Classification:** real reproducibility/compatibility risk not represented in the document  
**Disposition:** **open** — either pin local images to the tested production families or explicitly document
and continuously test the intentional version divergence.

Evidence:

- AppHost uses untagged `redis/redis-stack` and `falkordb/falkordb` images
  (`src/Hexalith.Memories.AppHost/Program.cs:131-140`, `276-280`). Production pins Redis Stack
  `7.4.0-v8` and FalkorDB `v4.12.0` by digest
  (`deploy/kubernetes/base/redis-statefulset.yaml:32-40`;
  `deploy/kubernetes/base/falkordb-statefulset.yaml:32-40`).
- The architecture names the products but neither their tested server versions nor this profile difference
  (`architecture.md:61-69`, `244-270`, `568-574`). A fresh local pull can therefore exercise a different
  server major/minor from production while still appearing architecture-compliant.

Primary sources (accessed 2026-09-09):

- Redis Stack 7.4 release notes:
  https://redis.io/docs/latest/operate/oss_and_stack/stack-with-enterprise/release-notes/redisstack/redisstack-7.4-release-notes/
- FalkorDB releases:
  https://github.com/FalkorDB/FalkorDB/releases

Acceptance condition: record the two environment policies and the compatibility test that permits any
intentional difference; otherwise use exact tags/digests in AppHost as well.

#### TC-08 — Dapr Conversation provider portability is directionally valid but overstated as a mature swap

**Classification:** unsupported fit/maturity claim  
**Disposition:** **defer** — keep the Dapr boundary, but qualify provider maturity and require per-provider
configuration/integration evidence before claiming interchangeability.

Evidence:

- The architecture says providers swap “by changing YAML only” and frames this as avoiding direct SDK
  lock-in (`architecture.md:568-574`, `1024-1131`). Current code does correctly target
  `DaprConversationClient` and the AppHost uses deterministic `conversation.echo`
  (`src/Hexalith.Memories.AppHost/Program.cs:263-274`).
- Dapr's official component catalogue lists OpenAI, Mistral, Anthropic, and Google AI Conversation
  components as **Alpha v1**; only the local echo component is listed stable. Different components also
  require provider-specific metadata/secrets. The code abstraction is real, but production-provider fit is
  not proven by a YAML boundary alone.

Primary source (accessed 2026-09-09):

- Dapr supported Conversation components:
  https://docs.dapr.io/reference/components-reference/supported-conversation/

Acceptance condition: mark production providers alpha, record the chosen provider's exact component schema
and integration evidence, and phrase portability as code isolation rather than proven drop-in equivalence.

### Low / informational

#### TC-09 — Core current pins exist and form a plausible supported baseline

**Classification:** verified fit; prevents stale wording from being mistaken for wholesale obsolescence  
**Disposition:** **ignore** as a defect; retain as evidence when updating the architecture.

- .NET 10 remains an active LTS line and current official downloads pair it with C# 14. The repo's
  SDK 10.0.400, `net10.0`, and C# 14 settings are coherent.
- Aspire.Hosting 13.5.3 exists and targets the current .NET generation. The exact CommunityToolkit Dapr
  prerelease also exists, although its prerelease status needs explicit governance (TC-03).
- Dapr .NET SDK 1.18.5 exists, supports .NET 10, and the CI runtime remains on Dapr's supported 1.18 line.
  Package and runtime patch numbers need not be identical; the architecture should simply stop conflating
  them.
- NRedisStack 1.7.4, StackExchange.Redis 3.1.31, NFalkorDB 1.2.0, xunit.v3 4.0.0,
  NSubstitute 6.2.0, and Shouldly 4.3.0 all exist in their official registries. Kreuzberg 4.10.2 also
  exists; 4.10.3 being available is ordinary one-patch lag, not demonstrated incompatibility.
- NFalkorDB remains the documented C# client for FalkorDB. That technical fit is independent of the server
  licence decision in TC-01.
- OpenBao 2.6 and PostgreSQL 18 still exist and fit their assigned roles. Their exact old patch images are
  the problem identified in TC-02, not product disappearance.

Additional primary source (accessed 2026-09-09):

- FalkorDB client documentation (NFalkorDB):
  https://docs.falkordb.com/getting-started/clients
- OpenBao installation/container registries:
  https://openbao.org/docs/install/
- PostgreSQL 18 release line:
  https://www.postgresql.org/docs/18/release.html

## Auditability result

The artifact does not meet the gate's “every committed technology/version decision was researched or
reality-checked” requirement. Its external references are sparse, its version table contains no source URL
or access date, and the blanket web-verification claim at `architecture.md:1685-1688` is not reproducible.
This review could verify that most named products/packages still exist, but it could not turn unsupported
claims (notably Dapr Agents GA, licensing mitigation, or production Conversation-provider equivalence) into
evidence.

## Required gate actions

1. Resolve TC-01 and TC-02 before declaring the architecture implementation-ready: licences and known
   security patch gaps are release-affecting.
2. Rebase the technical inventory on current repository authorities, while retaining March values only as
   explicitly historical narrative.
3. Reconcile binding topology/pattern decisions with compiled code: C# Conversation client versus Python
   Dapr Agents, Google/Ollama models, MediatR/FluentValidation, AccessTelemetry, MCP, EventStore, OpenBao,
   and PostgreSQL.
4. Add per-decision provenance: authoritative file, upstream primary URL, checked date, stability/maturity,
   and an upgrade/review trigger. Do not self-certify web research without those fields.
5. Re-run this lens after correction. Passing requires no critical/high open findings and no instruction to
   implement obsolete exact pins.

