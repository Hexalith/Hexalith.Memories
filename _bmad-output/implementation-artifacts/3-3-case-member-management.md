# Story 3.3: Case Member Management

Status: ready-for-dev

## Story

As a developer,
I want to add and remove members to a case,
so that I can control who has access to the knowledge within each case.

## Acceptance Criteria

1. **Given** an existing case, **When** I add a member by identity (user ID or role), **Then** the member is associated with the case **And** a `MemberAdded` activity event is recorded (FR36)
2. **Given** a case with members, **When** I remove a member by identity, **Then** the member is disassociated from the case **And** a `MemberRemoved` activity event is recorded
3a. **Given** a case with members, **When** I request the members endpoint, **Then** the full member list is returned via `GET /cases/{caseId}/members`
3b. **Given** a case with members, **When** I view case status, **Then** the `GET /cases/{caseId}/status` response includes a `MemberCount` field
4. **Given** an attempt to add a member that already exists in the case, **When** the operation is processed, **Then** it is idempotent -- no error, no duplicate entry

## Tasks / Subtasks

- [ ] Task 1: Create domain model contracts (AC: #1, #2, #3, #4)
  - [ ] 1.1 Create `CaseMember.cs` sealed record in `Contracts/V1/`
  - [ ] 1.2 Create `CaseMemberType.cs` enum in `Contracts/V1/`
  - [ ] 1.3 Create `AddCaseMemberInput.cs` sealed record in `Contracts/V1/`
  - [ ] 1.4 Register all new types in `MemoriesJsonContext.cs`
- [ ] Task 2: Add validation methods to `CaseValidator` (AC: #1, #2)
  - [ ] 2.1 Add `ValidateAddMember(tenantId, caseId, AddCaseMemberInput)` returning `ErrorResponse?`
  - [ ] 2.2 Add `ValidateRemoveMember(tenantId, caseId, memberId)` returning `ErrorResponse?`
  - [ ] 2.3 Validate CaseId with alphanumeric+hyphens regex (same as TenantIdGuard) -- prevents Redis key injection via `:` chars
  - [ ] 2.4 Validate MemberId with alphanumeric+hyphens+dots+underscores regex -- prevents log injection
- [ ] Task 3: Extend `CaseService` -- add 4 methods to EXISTING class, do NOT create CaseMemberService (AC: #1, #2, #3, #4)
  - [ ] 3.1 Add `AddMemberAsync(tenantId, caseId, AddCaseMemberInput, CancellationToken)` -- atomic idempotent via HSETNX
  - [ ] 3.2 Add `RemoveMemberAsync(tenantId, caseId, memberId, CancellationToken)` -- returns bool (true=removed, false=not found)
  - [ ] 3.3 Add `ListMembersAsync(tenantId, caseId, CancellationToken)` -- returns `List<CaseMember>`
  - [ ] 3.4 Add `GetMemberCountAsync(tenantId, caseId, CancellationToken)` -- returns `int` via `HashLengthAsync` (for AC #3 memberCount)
  - [ ] 3.5 Enforce member count limit: check `HashLengthAsync` before add, reject at 1000 with `MEMBER_LIMIT_EXCEEDED`
  - [ ] 3.6 Record activity events via existing `CaseActivityService` (await, matching `CreateCaseAsync` pattern)
  - [ ] 3.7 Add member methods after `GetCaseStatusAsync` -- group all member operations together
- [ ] Task 4: Create 3 API endpoints in `Program.cs` (AC: #1, #2, #3)
  - [ ] 4.1 `PUT /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` -- add/update member (201 new, 200 existing)
  - [ ] 4.2 `DELETE /api/tenants/{tenantId}/cases/{caseId}/members/{memberId}` -- remove member (204 or 404)
  - [ ] 4.3 `GET /api/tenants/{tenantId}/cases/{caseId}/members` -- list members (200)
- [ ] Task 5: Unit tests for Contracts serialization (AC: #1, #3)
  - [ ] 5.1 `CaseMemberSerializationTests.cs`
  - [ ] 5.2 `AddCaseMemberInputSerializationTests.cs`
  - [ ] 5.3 Add `CaseMemberType` to `EnumSerializationTests.cs`
- [ ] Task 6: Unit tests for CaseValidator member methods (AC: #1, #2)
  - [ ] 6.1 Add `ValidateAddMember_*` and `ValidateRemoveMember_*` tests to `CaseValidatorTests.cs`
- [ ] Task 7: Unit tests for CaseService member methods (AC: #1, #2, #3, #4)
  - [ ] 7.1 Add member operation tests to `CaseServiceTests.cs` with NSubstitute mocks
  - [ ] 7.2 Test idempotent add via HSETNX (returns false -> read existing, no activity event)
  - [ ] 7.3 Test remove returns false when member not found
  - [ ] 7.4 Test activity events recorded on add/remove
  - [ ] 7.5 Test list returns empty when no members
  - [ ] 7.6 Test member count limit: reject add when HashLengthAsync >= 1000
  - [ ] 7.7 Test when HashSetAsync throws, RecordEventAsync is never called
  - [ ] 7.8 Test idempotent fallback when HashGetAsync returns null after HSETNX false (delete-between-check race)
- [ ] Task 8: Integration tests (AC: #1, #2, #3, #4)
  - [ ] 8.1 Add member endpoint tests to `CaseEndpointIntegrationTests.cs`
  - [ ] 8.2 Verify `ListCasesAsync` returns exactly one case after adding members (`:members` key not counted as a case)
  - [ ] 8.3 Member limit integration test: add 1000 members in loop, verify 1001st returns 400 MEMBER_LIMIT_EXCEEDED (catches off-by-one and Redis version behavior)
  - [ ] 8.4 Concurrent HSETNX test: fire 10 parallel PUTs for same memberId, assert exactly 1 MemberAdded activity event in stream (proves atomicity)
- [ ] Task 9: Add deferred-work entry (non-blocking, do after all code tasks)
  - [ ] 9.1 Append to `deferred-work.md`: "Case deletion (Story 3.5) must cascade-delete `{tenantId}:case:{caseId}:members` key"

## Dev Notes

### Implementation Order

Task 1 -> 2 -> 3 -> 4 -> 5-9 (tests in parallel). NOT numeric order -- contracts and validation before service, service before endpoints.

### Required Import

`CaseService.cs` currently has NO `System.Text.Json` import. The new member methods use `JsonSerializer.Serialize` and `JsonSerializer.Deserialize`. Add `using System.Text.Json;` to the top of the file.

### AC3: memberCount on Case Details

AC3 requires case details to include member information. This is satisfied by two mechanisms:
1. **Dedicated endpoint:** `GET /cases/{caseId}/members` returns the full member list
2. **memberCount field:** Extend the existing `GetCaseAsync` and `GetCaseStatusAsync` to include a `MemberCount` property. Add `GetMemberCountAsync(tenantId, caseId)` to CaseService using `HashLengthAsync` on the members key. Pipe this into the `Case` record returned by `GetCaseAsync`. This requires adding a `MemberCount` property to the `Case` record or extending `CaseStatusDetail` -- use the same `with` pattern used for `MemoryUnitCount`.

**Simplest approach:** Add `MemberCount` to `CaseStatusDetail` only (not to `Case` base record). This keeps the `Case` record unchanged and puts member count alongside other health indicators. Update `GetCaseStatusAsync` to call `GetMemberCountAsync` in the same `Task.WhenAll` block.

**Exact record change** -- `CaseStatusDetail` currently has 11 constructor params (Id through FailedCount). Add `MemberCount` as the 12th parameter at the end:
```csharp
public sealed record CaseStatusDetail(
    string Id,
    string TenantId,
    string Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Description,
    CaseStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdated,
    int MemoryUnitCount,
    DateTimeOffset? LastActivityAt,
    int IndexedCount,
    int FailedCount,
    int MemberCount);  // <-- NEW: 12th param
```
This breaks the existing `GetCaseStatusAsync` call site in `CaseService.cs` (line 172) -- add `MemberCount: memberCountTask.Result` to the constructor call. It also breaks `CaseStatusDetailSerializationTests.cs` -- update the test to include the new parameter and verify `"memberCount"` appears in JSON output.

### Storage Design

**Members stored in Redis Hash** (not Set, not List):
- Key: `{tenantId}:case:{caseId}:members`
- Field: `{memberId}` (user ID or role string)
- Value: JSON string `{"memberType":"user","addedAt":"2026-04-12T00:00:00+00:00"}`

Rationale: Redis Hash gives O(1) member existence check (`HashExistsAsync`), O(1) add/remove (`HashSetAsync`/`HashDeleteAsync`), and O(N) list (`HashGetAllAsync`). Matches the existing Hash pattern used for case metadata.

**Idempotency (AC #4):** Use `HashSetAsync(key, field, value, When.NotExists)` (the Redis `HSETNX` command) for atomic idempotent add. Returns `true` if the field was created (new member), `false` if it already existed. On `false`, read the existing value with `HashGetAsync` and return it. Fire activity event ONLY when `HSETNX` returns `true`. This eliminates the TOCTOU race condition that exists with a separate `HashExistsAsync` + `HashSetAsync` check-then-write pattern.

**Member count limit (approximate):** Before attempting `HSETNX`, call `HashLengthAsync` to check current count. Reject with `MEMBER_LIMIT_EXCEEDED` (400) if count >= 1000. This prevents unbounded Hash growth and `HashGetAllAsync` latency spikes. The limit is approximate under concurrent load -- two concurrent adds can both pass the count check and both succeed, exceeding 1000 by a small margin. This is acceptable for MVP; a truly atomic enforcement would require a Lua script (`HLEN` + `HSETNX` in one call), which is not worth the complexity for a soft guard.

### Contract Specifications

```csharp
// Contracts/V1/CaseMemberType.cs
[JsonConverter(typeof(CamelCaseStringEnumConverter<CaseMemberType>))]
public enum CaseMemberType { User, Role }

// Contracts/V1/CaseMember.cs
public sealed record CaseMember(
    string MemberId,
    CaseMemberType MemberType,
    DateTimeOffset AddedAt);

// Contracts/V1/AddCaseMemberInput.cs
public sealed record AddCaseMemberInput(
    string MemberId,
    CaseMemberType MemberType);
```

Notes:
- `CaseMember` does NOT include TenantId or CaseId -- those are context from the URL path
- `AddCaseMemberInput` does NOT include TenantId/CaseId -- injected from route params (same pattern as `CreateCaseInput`)
- Use `CamelCaseStringEnumConverter<T>` for the enum (same converter used by `CaseStatus` and `CaseActivityEventType`)
- Follow copyright header pattern from existing files

### JsonContext Registration

Add to `MemoriesJsonContext.cs`:
```csharp
[JsonSerializable(typeof(AddCaseMemberInput))]
[JsonSerializable(typeof(CaseMember))]
[JsonSerializable(typeof(CaseMemberType))]
[JsonSerializable(typeof(List<CaseMember>))]
```

### Validation Rules

`ValidateAddMember(string tenantId, string caseId, AddCaseMemberInput input)`:
- TenantId: `TenantIdGuard.Validate(tenantId)` (catch ArgumentException -> INVALID_TENANT_ID)
- CaseId: not null/empty AND alphanumeric+hyphens only (-> INVALID_CASE_ID). This prevents Redis key injection via `:` chars in `{tenantId}:case:{caseId}:members`. Use regex `^[a-zA-Z0-9\-]+$` (same pattern as TenantIdGuard).
- MemberId: not null/whitespace, max 200 chars, alphanumeric+hyphens+dots+underscores only (-> INVALID_MEMBER_ID). Regex: `^[a-zA-Z0-9\-._]+$`. Prevents log injection via activity event description interpolation.
- MemberType: no explicit validation needed (enum deserialization handles invalid values)

`ValidateRemoveMember(string tenantId, string caseId, string memberId)`:
- TenantId: `TenantIdGuard.Validate(tenantId)`
- CaseId: not null/empty AND alphanumeric+hyphens only (same regex)
- MemberId: not null/whitespace, alphanumeric+hyphens+dots+underscores only (same regex)

New error codes: `INVALID_CASE_ID`, `INVALID_MEMBER_ID`, `MEMBER_NOT_FOUND`, `MEMBER_LIMIT_EXCEEDED`

### CaseService Extension

Add three methods to the existing `CaseService` class. Do NOT create a `CaseMemberService` -- keep all case operations in one class. If a `CaseMemberService.cs` file exists after implementation, something went wrong.

```csharp
private const int MaxMembersPerCase = 1000;

public async Task<(CaseMember Member, bool Created)> AddMemberAsync(
    string tenantId, string caseId, AddCaseMemberInput input, CancellationToken cancellationToken)
{
    IDatabase db = _redis.GetDatabase();
    string membersKey = $"{tenantId}:case:{caseId}:members";

    // Enforce member count limit before attempting add
    long currentCount = await db.HashLengthAsync(membersKey).ConfigureAwait(false);
    if (currentCount >= MaxMembersPerCase)
    {
        throw new InvalidOperationException($"Case '{caseId}' has reached the maximum of {MaxMembersPerCase} members.");
    }

    DateTimeOffset now = DateTimeOffset.UtcNow;
    var member = new CaseMember(input.MemberId, input.MemberType, now);
    string json = JsonSerializer.Serialize(member, MemoriesJsonContext.Options);

    // Atomic idempotent add via HSETNX -- no TOCTOU race
    bool created = await db.HashSetAsync(membersKey, input.MemberId, json, When.NotExists).ConfigureAwait(false);

    if (created)
    {
        // Activity event ONLY for new members -- await to match CreateCaseAsync pattern
        _ = await _activityService.RecordEventAsync(
            tenantId, caseId, CaseActivityEventType.MemberAdded, "system",
            $"Member '{input.MemberId}' ({input.MemberType}) added", null, cancellationToken).ConfigureAwait(false);

        return (member, true);
    }

    // HSETNX returned false -- member already existed. Read the stored version.
    // Edge case: member could have been deleted between HSETNX and HashGet (rare race).
    RedisValue existing = await db.HashGetAsync(membersKey, input.MemberId).ConfigureAwait(false);
    if (!existing.HasValue)
    {
        // Member was deleted between HSETNX check and read. Retry the add.
        // This is extremely rare; simply return the member we tried to create.
        await db.HashSetAsync(membersKey, input.MemberId, json).ConfigureAwait(false);
        _ = await _activityService.RecordEventAsync(
            tenantId, caseId, CaseActivityEventType.MemberAdded, "system",
            $"Member '{input.MemberId}' ({input.MemberType}) added", null, cancellationToken).ConfigureAwait(false);
        return (member, true);
    }

    CaseMember existingMember = JsonSerializer.Deserialize<CaseMember>(existing.ToString(), MemoriesJsonContext.Options)!;
    return (existingMember, false);
}

public async Task<bool> RemoveMemberAsync(
    string tenantId, string caseId, string memberId, CancellationToken cancellationToken)
{
    IDatabase db = _redis.GetDatabase();
    string membersKey = $"{tenantId}:case:{caseId}:members";

    bool removed = await db.HashDeleteAsync(membersKey, memberId).ConfigureAwait(false);
    if (removed)
    {
        _ = await _activityService.RecordEventAsync(
            tenantId, caseId, CaseActivityEventType.MemberRemoved, "system",
            $"Member '{memberId}' removed", null, cancellationToken).ConfigureAwait(false);
    }

    return removed;
}

public async Task<List<CaseMember>> ListMembersAsync(
    string tenantId, string caseId, CancellationToken cancellationToken)
{
    IDatabase db = _redis.GetDatabase();
    string membersKey = $"{tenantId}:case:{caseId}:members";

    HashEntry[] entries = await db.HashGetAllAsync(membersKey).ConfigureAwait(false);
    List<CaseMember> members = new(entries.Length);
    foreach (HashEntry entry in entries)
    {
        CaseMember? parsed = JsonSerializer.Deserialize<CaseMember>(
            entry.Value.ToString(), MemoriesJsonContext.Options);
        if (parsed is not null)
        {
            members.Add(parsed);
        }
    }

    return members.OrderBy(m => m.AddedAt).ToList();
}
```

**Why `(CaseMember, bool)` return tuple for AddMemberAsync:** The `Created` flag lets the endpoint return 201 (new) vs 200 (existing), giving callers useful information without inspecting the member.

**Why `await` activity instead of fire-and-forget:** `CreateCaseAsync` (line 70 of CaseService.cs) already awaits `RecordEventAsync`. Matching this pattern prevents a second convention in the same class. The call is still safe -- `CaseActivityService.RecordEventAsync` catches all exceptions internally.

### API Endpoints

Add to `Program.cs` after the existing case activity endpoint:

**PUT** `/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}`:
- Inject: `CaseService`, `CancellationToken`
- Deserialize body as `AddCaseMemberInput` (MemberType field only -- MemberId comes from route)
- Override `input.MemberId` with route param `memberId` (same trust-route-over-body pattern as cases)
- Validate via `CaseValidator.ValidateAddMember(tenantId, caseId, input)`
- Verify case exists via `caseService.GetCaseAsync()` -- return 404 CASE_NOT_FOUND if not
- Call `caseService.AddMemberAsync()` -- returns `(member, created)` tuple
- If `created`: return `Results.Created($".../members/{memberId}", member)` (201)
- If not `created`: return `Results.Ok(member)` (200 -- idempotent, member already existed)
- If `InvalidOperationException` (limit exceeded): return 400 with `MEMBER_LIMIT_EXCEEDED`

**Why PUT instead of POST:** PUT is naturally idempotent (RFC 9110). The memberId is in the URL, making it the resource identifier. This gives clean 201/200 semantics without the POST-to-collection ambiguity.

**PUT is create-if-absent, NOT upsert:** If a member already exists with `memberType=User` and a client PUTs `memberType=Role`, the existing member is returned unchanged (HSETNX no-ops). The client's memberType is silently discarded. This is correct idempotency behavior -- the first write wins. If upsert semantics are needed in the future, add a separate `PATCH /members/{memberId}` endpoint.

**DELETE** `/api/tenants/{tenantId}/cases/{caseId}/members/{memberId}`:
- Inject: `CaseService`, `CancellationToken`
- Validate via `CaseValidator.ValidateRemoveMember(tenantId, caseId, memberId)`
- Verify case exists -- return 404 CASE_NOT_FOUND if not
- Call `caseService.RemoveMemberAsync()`
- Return `Results.NoContent()` (204) if removed, `Results.NotFound(MEMBER_NOT_FOUND)` if not

**GET** `/api/tenants/{tenantId}/cases/{caseId}/members`:
- Inject: `CaseService`, `CancellationToken`
- Validate tenantId and caseId (both with alphanumeric+hyphens regex)
- Verify case exists -- return 404 CASE_NOT_FOUND if not
- Call `caseService.ListMembersAsync()`
- Return `Results.Ok(members)`

### Error Code Registry

Existing codes (unchanged):
- `INVALID_TENANT_ID` (400)
- `CASE_NOT_FOUND` (404)

New codes for this story:
- `INVALID_CASE_ID` (400) -- "CaseId must be alphanumeric with hyphens only"
- `INVALID_MEMBER_ID` (400) -- "MemberId must be alphanumeric with hyphens, dots, and underscores only, max 200 characters"
- `MEMBER_NOT_FOUND` (404) -- "Member '{memberId}' is not in case '{caseId}'"
- `MEMBER_LIMIT_EXCEEDED` (400) -- "Case '{caseId}' has reached the maximum of 1000 members"

### Critical Anti-Patterns to Avoid

1. **No DAPR Actor** for member state -- simple Redis Hash operations, no per-entity statefulness needed
2. **No DAPR Workflow** for add/remove -- single-step operations, no orchestration needed
3. **No KEYS command** -- member key is deterministic (`{tenantId}:case:{caseId}:members`), no scanning
4. **No separate CaseMemberService** -- add methods to existing `CaseService`. If you create a `CaseMemberService.cs`, you are doing it wrong.
5. **No graph nodes for members** -- members are authorization metadata, not knowledge-graph entities. No graph traversal use case exists. If per-member content filtering ever becomes a requirement, this would change.
6. **Never trust TenantId/CaseId/MemberId from request body** -- always take from route parameters
7. **No check-then-write for idempotent add** -- use `HSETNX` (atomic). Never use `HashExistsAsync` + `HashSetAsync` separately (TOCTOU race).
8. **No duplicate activity events on idempotent add** -- fire activity ONLY when `HSETNX` returns `true` (new member)
9. **No unbounded member growth** -- enforce `HashLengthAsync` < 1000 before every add
10. **No special characters in CaseId** -- validate with regex `^[a-zA-Z0-9\-]+$` to prevent Redis key injection

### Architecture Decision Records

**ADR-1: Redis Hash for member storage (not Set, not Graph)**
Members stored in Redis Hash with field=memberId, value=JSON. Redis Set rejected (can't store metadata like memberType/addedAt). FalkorDB graph rejected (members are authorization metadata, not knowledge-graph content -- coupling auth to retrieval pipeline would inflate traversal cost). JSON values are a deliberate evolution from the flat Hash entries used for case metadata; this allows schema evolution (adding fields like `addedBy`, `permissions`) without migration. A flat pipe-delimited format (`"user|2026-04-12T..."`) scores higher on codebase consistency and simplicity (comparative analysis: 87 vs 67), but JSON was chosen because schema evolution outweighs pattern consistency for a field that will grow. If member metadata remains at 2 fields after Story 3.5, revisit. Known limitation: no reverse index for "list all cases for user X" -- requires SCAN or separate index key if needed.

**ADR-2: Extend CaseService (not new service)**
Member operations need the same Redis/FalkorDB connections, key-derivation logic, and case-existence validation. Separate service would duplicate dependencies or create lateral coupling. CaseService stays manageable (~300 lines after changes). Rule: if a 4th sub-resource appears, extract behind `ICaseRepository`.

**ADR-3: PUT with 201/200 (not POST with uniform 200)**
PUT is naturally idempotent per RFC 9110. MemberId in URL makes it the resource identifier. 201 signals new creation; 200 signals existing member returned. Callers get useful information without extra round trips.

### Test Patterns

Follow established patterns from stories 3.1 and 3.2:

**Serialization tests** (`Contracts.Tests/V1/`):
- Use `MemoriesJsonContext.Options` for serialization roundtrip
- Verify `camelCase` property names in JSON output
- Verify enum serializes as camelCase string (e.g., `"user"`, `"role"`)
- `[Fact]` per scenario, descriptive method names
- Use `Shouldly` assertions: `result.ShouldBe(...)`, `json.ShouldContain(...)`

**Validator tests** (`Server.Tests/Cases/CaseValidatorTests.cs`):
- Extend existing file (do NOT create new file)
- Test each validation rule: valid input -> null, invalid -> ErrorResponse with correct code
- Test boundary: MemberId exactly 200 chars (valid), 201 chars (invalid)

**Service tests** (`Server.Tests/Cases/CaseServiceTests.cs`):
- Extend existing file (do NOT create new file)
- Use `NSubstitute` mocks for `IConnectionMultiplexer`, `IDatabase`, `IGraphQueryBuilder`, `CaseActivityService`
- Mock `IConnectionMultiplexer.GetDatabase()` to return mocked `IDatabase`
- Test new add: mock `HashSetAsync(When.NotExists)` returning `true` -> verify activity event recorded, `Created=true`
- Test idempotent add: mock `HashSetAsync(When.NotExists)` returning `false`, mock `HashGetAsync` returning existing JSON -> verify no activity event, `Created=false`
- Test member limit: mock `HashLengthAsync` returning 1000 -> verify `InvalidOperationException`
- Test remove success/not-found
- Test list empty/populated
- Test when `HashSetAsync` throws -> verify `RecordEventAsync` never called (activity only after successful write)

**Integration tests** (`IntegrationTests/Cases/CaseEndpointIntegrationTests.cs`):
- Extend existing file
- Full HTTP roundtrip: create case -> PUT member -> list members -> DELETE member -> verify removed
- Test idempotent PUT (add same member twice, verify 201 then 200, list shows one entry)
- Test 404 when case doesn't exist
- Test `ListCasesAsync` returns exactly one case after adding members (`:members` key scan not mistaken for a case)

### Previous Story Intelligence

**From Story 3.1:**
- `Shouldly.Case` naming conflict: qualify as `Shouldly.Case.Sensitive` in test files where needed
- `ByteAether.Ulid` used for ID generation (not needed for members -- memberId is user-provided)
- All new contracts are sealed records with JSON serialization support
- `ParseCaseFromHash` pattern shows how to read Redis Hash entries
- 534 tests passing baseline (now 581 after story 3.2)

**From Story 3.2:**
- `CaseActivityEventType` already has `MemberAdded` and `MemberRemoved` values -- no enum changes needed
- `CaseActivityService` singleton registration pattern (already done)
- `CaseActivityService.RecordEventAsync` catches all exceptions internally -- always safe to call
- `RecordCaseActivityActivity` exists for workflow contexts (not needed here -- member operations are synchronous)
- Known scope gap from 3.2: "Membership change activity (deferred to Story 3.3)" -- this story closes that gap

**Activity recording pattern clarification:** Two patterns exist in the codebase:
- **Await pattern** (CaseService): `_ = await _activityService.RecordEventAsync(...)` -- used in `CreateCaseAsync` (line 70). The result is discarded but the call is awaited.
- **Fire-and-forget pattern** (Program.cs search endpoint): `_ = activityService.RecordEventAsync(...)` -- no await, truly async fire-and-forget.
- **This story uses the await pattern** because member methods live in CaseService alongside CreateCaseAsync. Consistency within a single class is more important than matching the search endpoint pattern. Both patterns are safe because `RecordEventAsync` catches all exceptions internally.

**From deferred-work.md:**
- CaseId not validated for special characters globally -- this story adds CaseId validation specifically in member endpoints (where caseId is used to construct Redis keys). This does NOT change the existing behavior in other endpoints; it only hardens the new member key paths against Redis key injection.

**Cascade cleanup gap:**
- When Story 3.5 (case deletion) is implemented, it MUST also delete `{tenantId}:case:{caseId}:members` and `{tenantId}:case:{caseId}:activity` keys. This story adds a deferred-work entry to track this.

### Project Structure Notes

New files (3 contracts + 2 test files = 5):
- `src/Hexalith.Memories.Contracts/V1/CaseMember.cs`
- `src/Hexalith.Memories.Contracts/V1/CaseMemberType.cs`
- `src/Hexalith.Memories.Contracts/V1/AddCaseMemberInput.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/CaseMemberSerializationTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/AddCaseMemberInputSerializationTests.cs`

Modified files (11):
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (add 4 JsonSerializable attributes)
- `src/Hexalith.Memories.Contracts/V1/CaseStatusDetail.cs` (add `MemberCount` as 12th constructor param)
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` (add `using System.Text.Json;`, add 4 methods + MaxMembersPerCase constant, update GetCaseStatusAsync to include MemberCount -- NO new file)
- `src/Hexalith.Memories.Server/Cases/CaseValidator.cs` (add 2 validation methods with CaseId/MemberId regex)
- `src/Hexalith.Memories.Server/Program.cs` (add 3 endpoints: PUT, DELETE, GET)
- `tests/Hexalith.Memories.Contracts.Tests/V1/EnumSerializationTests.cs` (add CaseMemberType)
- `tests/Hexalith.Memories.Contracts.Tests/V1/CaseStatusDetailSerializationTests.cs` (update for new MemberCount param -- WILL BREAK without this)
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseValidatorTests.cs` (extend with member validation tests)
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs` (extend with HSETNX, limit, activity, memberCount, fallback-race tests)
- `tests/Hexalith.Memories.IntegrationTests/Cases/CaseEndpointIntegrationTests.cs` (extend with PUT/DELETE roundtrips, limit test, concurrent HSETNX test)
- `_bmad-output/implementation-artifacts/deferred-work.md` (add cascade cleanup entry)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3, Story 3.3]
- [Source: _bmad-output/planning-artifacts/prd.md#FR28, FR29]
- [Source: _bmad-output/planning-artifacts/architecture.md#Cases, Code Style, Testing Standards]
- [Source: _bmad-output/implementation-artifacts/3-1-create-and-list-cases.md#Dev Notes, File List]
- [Source: _bmad-output/implementation-artifacts/3-2-case-status-and-activity.md#Known Scope Gaps, Dev Notes]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#CaseId validation deferred]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
