# Sprint Change Proposal — Replace Apache Tika with Kreuzberg

**Date:** 2026-03-28
**Triggered by:** Jerome (change request — replace Tika with Kreuzberg for native C# content extraction)
**Sprint impact:** Epic 1, Story 1.3 (ready-for-dev, not yet implemented)

---

## Section 1: Issue Summary

**Problem Statement:** Architecture Decision D13 specifies Apache Tika as an external Java container for content extraction. This requires an additional Docker container (~256MB + JVM), HTTP round-trips for every extraction, container health checks, and deployment complexity (port mapping, WaitFor dependencies, localhost limitations in containerized deployment).

**Change Request:** Replace Apache Tika with **Kreuzberg**, a Rust-core document intelligence framework with native C# bindings via P/Invoke. Kreuzberg runs entirely in-process — no Docker container, no HTTP overhead, no JVM.

**Evidence supporting this change:**
- Kreuzberg v4.6.3 (released 2026-03-27) supports 91+ formats including all MVP-required formats (PDF, plain text, markdown)
- Native C# API via `KreuzbergClient` static class — `ExtractFileAsync()`, `ExtractBytesAsync()`, MIME detection
- .NET 10.0+ required — **compatible** with project's `net10.0` target framework
- NuGet package: `Kreuzberg` (158 MB, includes prebuilt native binaries for Linux/macOS/Windows)
- No external Docker container needed — fully in-process
- MIT licensed — compatible with open-source project

**Timing:** Story 1.3 is `ready-for-dev` but **no code has been written yet**. No rollback needed. This is the optimal time for this change.

---

## Section 2: Impact Analysis

### Epic Impact

| Epic | Impact | Details |
|------|--------|---------|
| **Epic 1** | Direct — Story 1.3 rewrite | Story 1.3 must be rewritten for Kreuzberg. Epic description references "Tika" explicitly in infrastructure spine list. |
| **Epic 6** | Minor reference | Story 6.1 acceptance criteria mentions "`ExtractContentActivity` (Tika)" — needs text update. Activity itself unchanged. |
| All others | None | No other epics reference Tika. |

### Story Impact

| Story | Status | Impact |
|-------|--------|--------|
| **1.3** | ready-for-dev | **Full rewrite** — remove all Tika-specific implementation (container, HTTP client, health check). Replace with Kreuzberg NuGet package, in-process extraction client. |
| **1.1** | done | **No code change needed** — Story 1.1 explicitly excluded Tika (`"DO NOT add Tika container — that's Story 1.3"`). No Tika code in source tree. |
| **1.6** | backlog | **No change** — IngestionWorkflow calls `ExtractContentActivity` regardless of backend. Activity interface unchanged. |
| **6.1** | backlog | **Text-only** — update acceptance criteria wording from "(Tika)" to "(Kreuzberg)" |

### Artifact Conflicts

| Artifact | Sections Affected | Severity |
|----------|------------------|----------|
| **Architecture (architecture.md)** | Decision D13, Deployment Topology, Cross-Component Dependencies, Error Handling (Layer 3), Activity definition examples, Project Structure tree, Service Boundaries table, Data Flow diagram | **High** — 10+ sections reference "Tika" |
| **Epics (epics.md)** | Epic 1 description (×2), Story 1.3 (full), Story 6.1 (reference) | **Medium** — story rewrite + text updates |
| **Story file (1-3-content-extraction-via-tika.md)** | Entire file | **High** — complete replacement |
| **Sprint status (sprint-status.yaml)** | Story key `1-3-content-extraction-via-tika` | **Low** — rename story key |
| **PRD (prd.md)** | None | **None** — PRD says "extract text" without specifying Tika |
| **Story 1.1 file** | Reference exclusion line | **Low** — update exclusion wording |

### Technical Impact

| Area | Impact |
|------|--------|
| **Infrastructure** | **Simplified** — removes 1 Docker container from deployment topology. AppHost no longer needs Tika container resource or `WaitFor(tika)`. |
| **Dependencies** | **New:** `Kreuzberg` NuGet package (158 MB, native binaries). **Removed:** No Tika HTTP client, no Tika health check. Add to `Directory.Packages.props`. |
| **Content extraction client** | **Rewrite** — replace HTTP-based `ContentExtractionClient` with in-process `KreuzbergClient.ExtractBytesAsync()`. Same contract interface (`ExtractionInput → ExtractionResult`). |
| **Health checks** | **Remove** `TikaHealthCheck` — no external service to check. Kreuzberg is in-process (if it loads, it works). |
| **Error handling** | **Simplified** — no more `HttpRequestException` from HTTP calls. Kreuzberg throws native exceptions on extraction failure. Still propagated to DAPR Workflow retry. |
| **Memory model** | **Changed** — extraction now runs in-process in Server memory instead of isolated Tika container. One of Tika's benefits was resource isolation. Kreuzberg's Rust core claims 60-90% less memory than Java alternatives, mitigating this. |
| **Package size** | 158 MB NuGet package (native binaries for 3 platforms) vs ~1GB Tika Docker image. Net reduction in deployment size. |

---

## Section 3: Recommended Approach

### Selected Path: **Direct Adjustment** (Option 1)

Modify Story 1.3 and update all referencing artifacts. No rollback needed (no code exists). No MVP scope change.

**Rationale:**
1. **No code to roll back** — Story 1.3 is `ready-for-dev`, untouched
2. **Same contract surface** — `ExtractionInput → ExtractionResult` is unchanged. `ExtractContentActivity` still wraps a client. Downstream stories (1.4, 1.5, 1.6) are unaffected.
3. **Simplifies infrastructure** — removes 1 container, 1 health check, 1 HTTP client, port mapping, WaitFor dependency, localhost deployment limitation
4. **Better developer experience** — `dotnet run` just works, no Docker image pull for Tika
5. **.NET 10.0 compatible** — project already targets `net10.0`

**Effort estimate:** Low — documentation updates + story rewrite (no existing code to migrate)
**Risk level:** Low-Medium
- **Low risk:** No breaking changes to contracts or activity interfaces
- **Medium risk:** Kreuzberg is young (~8.5K NuGet downloads vs battle-tested Tika). Format coverage is 91 formats vs Tika's 1000+. Sufficient for MVP (PDF, text, markdown) but may need evaluation for exotic formats later.

**Timeline impact:** None — Story 1.3 hasn't started.

---

## Section 4: Detailed Change Proposals

### 4.1 Architecture Document (`architecture.md`)

#### Change A1: Decision D13

**Section:** Data Architecture decisions table

**OLD:**
```
| D13 | Content extraction | External Apache Tika container | 1000+ format support, resource isolation (extraction doesn't spike Server memory), battle-tested. | Deployment topology (+1 container), AppHost, pipeline `extracting` stage, health checks |
```

**NEW:**
```
| D13 | Content extraction | Kreuzberg NuGet package (in-process, Rust core via P/Invoke) | 91+ format support, native C# API, no Docker container overhead, zero network latency, MIT licensed. | Server project dependency, pipeline `extracting` stage |
```

**Rationale:** Core technology decision changes from external container to in-process library.

#### Change A2: Deployment Topology table

**OLD:**
```
| Apache Tika | Content extraction service | ~256MB | 9998 |
```

**NEW:** Remove this row entirely.

**Rationale:** No container to deploy.

#### Change A3: Complete Decision Registry

**OLD:**
```
| D13 | External Tika for content extraction | Resource isolation, format coverage | MVP |
```

**NEW:**
```
| D13 | Kreuzberg for content extraction (in-process) | Native C#, no container overhead, 91+ formats | MVP |
```

#### Change A4: Cross-Component Dependencies

**OLD:**
```
Server Workflows → Activities → {Tika, Embedding API, Redis, FalkorDB}
```

**NEW:**
```
Server Workflows → Activities → {Kreuzberg (in-process), Embedding API, Redis, FalkorDB}
```

#### Change A5: Error Handling Pattern (Layer 3)

**OLD:**
```
3. **Infrastructure:** Exceptions only for truly exceptional conditions (Redis down, Tika unreachable)
```

**NEW:**
```
3. **Infrastructure:** Exceptions only for truly exceptional conditions (Redis down, embedding API unreachable)
```

#### Change A6: Activity definition commentary

**OLD:**
```
Activities call external services (Tika, embedding API, Redis, FalkorDB) — workflows never call external services directly.
```

**NEW:**
```
Activities call services (Kreuzberg, embedding API, Redis, FalkorDB) — workflows never call external services directly.
```

#### Change A7: Project Structure tree

**OLD:**
```
│   │   │   │   ├── ExtractContentActivity.cs       # Calls Tika
```

**NEW:**
```
│   │   │   │   ├── ExtractContentActivity.cs       # Calls Kreuzberg (in-process)
```

#### Change A8: Service Boundaries table

**OLD:**
```
| Memories Server | C# | Domain logic, workflows, actors, search, tenants | Redis, FalkorDB, Tika, Embedding API, AI Agent Service | DAPR workflows/actors/state/service-invocation, HTTP |
...
| Tika | Java | Content extraction | — (stateless) | HTTP |
```

**NEW:**
```
| Memories Server | C# | Domain logic, workflows, actors, search, tenants | Redis, FalkorDB, Embedding API, AI Agent Service | DAPR workflows/actors/state/service-invocation, HTTP |
```
Remove the Tika service row entirely.

**Rationale:** Kreuzberg is in-process, not a separate service.

#### Change A9: Data Flow diagram

**OLD:**
```
    3. ExtractContentActivity → Tika (HTTP)
```

**NEW:**
```
    3. ExtractContentActivity → Kreuzberg (in-process)
```

#### Change A10: Test mock reference

**OLD:**
```
Mock external dependencies (Tika, embedding API, Redis, FalkorDB).
```

**NEW:**
```
Mock external dependencies (embedding API, Redis, FalkorDB). Kreuzberg is in-process — mock via interface wrapper or test with real library.
```

---

### 4.2 Epics Document (`epics.md`)

#### Change E1: Epic 1 description (appears twice)

**OLD:**
```
...infrastructure spine: Aspire AppHost, DAPR Workflows (IngestionWorkflow with saga/compensation), Contracts, Redis (RediSearch + Vector), FalkorDB, Tika, git submodules, and the IndexGraphActivity.
```

**NEW:**
```
...infrastructure spine: Aspire AppHost, DAPR Workflows (IngestionWorkflow with saga/compensation), Contracts, Redis (RediSearch + Vector), FalkorDB, Kreuzberg, git submodules, and the IndexGraphActivity.
```

**Rationale:** Infrastructure spine listing.

#### Change E2: Story 1.3 title and content

**OLD:**
```
### Story 1.3: Content Extraction via Tika
...using Apache Tika...
```

**NEW:**
```
### Story 1.3: Content Extraction via Kreuzberg
...using Kreuzberg...
```

Full acceptance criteria rewrite — remove all Tika container/port references. Replace with in-process Kreuzberg extraction.

#### Change E3: Story 6.1 acceptance criteria reference

**OLD:**
```
passes it through `ExtractContentActivity` (Tika)
```

**NEW:**
```
passes it through `ExtractContentActivity` (Kreuzberg)
```

#### Change E4: Requirements traceability

**OLD:**
```
- FR4: Epic 1 — Text extraction (Tika)
```

**NEW:**
```
- FR4: Epic 1 — Text extraction (Kreuzberg)
```

---

### 4.3 Story File Replacement

**Action:** Replace `1-3-content-extraction-via-tika.md` with `1-3-content-extraction-via-kreuzberg.md`

Key changes in the rewritten story:
- **Remove:** Task 1 (Tika container in AppHost), Task 6 (Tika health check), all HTTP client code
- **Add:** Task for adding `Kreuzberg` NuGet package to `Directory.Packages.props`
- **Rewrite:** `ContentExtractionClient` from HTTP-based to in-process `KreuzbergClient.ExtractBytesSync()` wrapper
- **Simplify:** Error handling (no `HttpRequestException` from HTTP calls — native exceptions instead)
- **Update:** Acceptance criteria to remove container/port references
- **Keep:** `ExtractionInput`/`ExtractionResult` contracts unchanged, `ExtractContentActivity` pattern unchanged, test structure unchanged

**Reference:** Full implementation approach documented in `_bmad-output/planning-artifacts/research/technical-kreuzberg-ocr-research-2026-03-28.md` § Implementation Approaches

---

### 4.4 Sprint Status (`sprint-status.yaml`)

**OLD:**
```yaml
1-3-content-extraction-via-tika: ready-for-dev
```

**NEW:**
```yaml
1-3-content-extraction-via-kreuzberg: ready-for-dev
```

---

### 4.5 PRD (`prd.md`)

#### Change P1: Additional Requirements — Technology Decisions

**OLD:**
```
- External Apache Tika container for content extraction (D13)
```

**NEW:**
```
- Kreuzberg NuGet package for content extraction — in-process, Rust core via P/Invoke (D13)
```

**Rationale:** PRD references architecture decisions by ID. FR4 itself is technology-agnostic and needs no change.

---

### 4.6 Story 1.1 reference update

**OLD:**
```
- **DO NOT add Tika container** — that's Story 1.3
```

**NEW:**
```
- **DO NOT add Kreuzberg NuGet package** — that's Story 1.3
```

---

## Section 5: Implementation Handoff

### Change Scope: **Minor**

This change can be implemented directly by the development team. No backlog reorganization or strategic replan needed.

### Handoff Plan

| Step | Who | Action |
|------|-----|--------|
| 1 | **Scrum Master (Bob)** | Update architecture.md with all A1-A10 changes (Proposals 1-5) |
| 2 | **Scrum Master (Bob)** | Update epics.md with E1-E4 changes (Proposals 6-7) |
| 3 | **Scrum Master (Bob)** | Update prd.md with P1 change (Proposal 8) |
| 4 | **Scrum Master (Bob)** | Create new story file `1-3-content-extraction-via-kreuzberg.md` (Proposal 9) |
| 5 | **Scrum Master (Bob)** | Remove old story file `1-3-content-extraction-via-tika.md` |
| 6 | **Scrum Master (Bob)** | Update sprint-status.yaml |
| 7 | **Scrum Master (Bob)** | Update story 1.1 reference |
| 8 | **Developer (Amelia)** | Implement updated Story 1.3 per new spec |

### Success Criteria

1. All artifact references to "Tika" replaced with "Kreuzberg" (or removed where the concept no longer applies)
2. Story 1.3 rewritten with Kreuzberg-specific implementation guidance
3. Architecture Decision D13 updated to reflect in-process extraction
4. PRD Additional Requirements updated to match D13
5. Deployment topology simplified (one fewer container)
6. No contract changes — `ExtractionInput`/`ExtractionResult` remain identical
7. Story 1.3 implementable as `ready-for-dev` after artifact updates

---

## Appendix: Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Kreuzberg library immaturity (~8.5K downloads) | Medium | Medium | Pin to specific version. MVP only needs PDF/text/markdown — well-tested formats. Monitor for issues. |
| 158 MB NuGet package size | Low | Low | Acceptable for server-side deployment. Not a client library. |
| Loss of resource isolation (extraction in-process) | Low | Low | Kreuzberg's Rust core is memory-efficient. Monitor Server memory during load tests. If extraction spikes memory, consider running extraction in a worker process. |
| Format coverage gap (91 vs 1000+) | Low | Low | MVP requires only PDF, plain text, markdown — all supported. Evaluate if exotic formats needed post-MVP. |

---

*Generated: 2026-03-28 | Workflow: Correct Course | Change scope: Minor*
