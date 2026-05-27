namespace Hexalith.Memories.Server.Tests.Cases;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Cases;

using Shouldly;

public class CaseValidatorTests
{
    [Fact]
    public void ValidInput_ShouldReturnNull()
    {
        var input = new CreateCaseInput("tenant-1", "Valid Name", "Optional description");
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldBeNull();
    }

    [Fact]
    public void NullDescription_ShouldBeValid()
    {
        var input = new CreateCaseInput("tenant-1", "Valid Name", null);
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyTenantId_ShouldReturnError(string tenantId)
    {
        var input = new CreateCaseInput(tenantId, "Valid Name", null);
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Fact]
    public void InvalidTenantIdCharacters_ShouldReturnError()
    {
        var input = new CreateCaseInput("tenant_with_underscores!", "Valid Name", null);
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyName_ShouldReturnError(string name)
    {
        var input = new CreateCaseInput("tenant-1", name, null);
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_CASE_NAME");
    }

    [Fact]
    public void NameExceeding200Chars_ShouldReturnError()
    {
        string longName = new('A', 201);
        var input = new CreateCaseInput("tenant-1", longName, null);
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_CASE_NAME");
    }

    [Fact]
    public void NameExactly200Chars_ShouldBeValid()
    {
        string maxName = new('A', 200);
        var input = new CreateCaseInput("tenant-1", maxName, null);
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldBeNull();
    }

    [Fact]
    public void DescriptionExceeding2000Chars_ShouldReturnError()
    {
        string longDesc = new('B', 2001);
        var input = new CreateCaseInput("tenant-1", "Valid Name", longDesc);
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_CASE_DESCRIPTION");
    }

    [Fact]
    public void DescriptionExactly2000Chars_ShouldBeValid()
    {
        string maxDesc = new('B', 2000);
        var input = new CreateCaseInput("tenant-1", "Valid Name", maxDesc);
        ErrorResponse? result = CaseValidator.ValidateCreateCase(input);

        result.ShouldBeNull();
    }

    // --- ValidateAddMember tests ---

    [Fact]
    public void ValidateAddMember_ValidInput_ShouldReturnNull()
    {
        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant-1", "case-001", input);

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateAddMember_InvalidTenantId_ShouldReturnError()
    {
        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant_bad!", "case-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("case:inject")]
    [InlineData("case_underscore")]
    public void ValidateAddMember_InvalidCaseId_ShouldReturnError(string caseId)
    {
        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant-1", caseId, input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_CASE_ID");
    }

    [Fact]
    public void ValidateAddMember_ValidCaseIdWithHyphens_ShouldReturnNull()
    {
        var input = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant-1", "my-case-123", input);

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateAddMember_EmptyMemberId_ShouldReturnError(string memberId)
    {
        var input = new AddCaseMemberInput(memberId, CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant-1", "case-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMBER_ID");
    }

    [Fact]
    public void ValidateAddMember_MemberIdExceeding200Chars_ShouldReturnError()
    {
        string longId = new('a', 201);
        var input = new AddCaseMemberInput(longId, CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant-1", "case-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMBER_ID");
    }

    [Fact]
    public void ValidateAddMember_MemberIdExactly200Chars_ShouldBeValid()
    {
        string maxId = new('a', 200);
        var input = new AddCaseMemberInput(maxId, CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant-1", "case-001", input);

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("user:colon")]
    [InlineData("user/slash")]
    [InlineData("user space")]
    [InlineData("user@at")]
    public void ValidateAddMember_MemberIdWithInvalidChars_ShouldReturnError(string memberId)
    {
        var input = new AddCaseMemberInput(memberId, CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant-1", "case-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMBER_ID");
    }

    [Theory]
    [InlineData("user-alice")]
    [InlineData("user.alice")]
    [InlineData("user_alice")]
    [InlineData("alice123")]
    [InlineData("ALICE")]
    [InlineData("user-alice.org_name")]
    public void ValidateAddMember_MemberIdWithValidChars_ShouldReturnNull(string memberId)
    {
        var input = new AddCaseMemberInput(memberId, CaseMemberType.User);
        ErrorResponse? result = CaseValidator.ValidateAddMember("tenant-1", "case-001", input);

        result.ShouldBeNull();
    }

    // --- ValidateRemoveMember tests ---

    [Fact]
    public void ValidateRemoveMember_ValidInput_ShouldReturnNull()
    {
        ErrorResponse? result = CaseValidator.ValidateRemoveMember("tenant-1", "case-001", "user-alice");

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateRemoveMember_InvalidTenantId_ShouldReturnError()
    {
        ErrorResponse? result = CaseValidator.ValidateRemoveMember("bad!", "case-001", "user-alice");

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Fact]
    public void ValidateRemoveMember_InvalidCaseId_ShouldReturnError()
    {
        ErrorResponse? result = CaseValidator.ValidateRemoveMember("tenant-1", "case:bad", "user-alice");

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_CASE_ID");
    }

    [Fact]
    public void ValidateRemoveMember_InvalidMemberId_ShouldReturnError()
    {
        ErrorResponse? result = CaseValidator.ValidateRemoveMember("tenant-1", "case-001", "user:bad");

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMBER_ID");
    }

    // --- ValidateMemoryUnitId tests ---

    [Theory]
    [InlineData("01JQWXYZ1234567890ABCDEF")]
    [InlineData("mu-001")]
    [InlineData("simple123")]
    public void ValidateMemoryUnitId_ValidId_ShouldReturnNull(string memoryUnitId)
    {
        ErrorResponse? result = CaseValidator.ValidateMemoryUnitId(memoryUnitId);

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateMemoryUnitId_NullOrEmpty_ShouldReturnError(string? memoryUnitId)
    {
        ErrorResponse? result = CaseValidator.ValidateMemoryUnitId(memoryUnitId!);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMORY_UNIT_ID");
        result.Message.ShouldContain("required");
    }

    [Theory]
    [InlineData("mu:colon")]
    [InlineData("mu/slash")]
    [InlineData("mu space")]
    [InlineData("mu@at")]
    [InlineData("mu_underscore")]
    public void ValidateMemoryUnitId_InvalidChars_ShouldReturnError(string memoryUnitId)
    {
        ErrorResponse? result = CaseValidator.ValidateMemoryUnitId(memoryUnitId);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMORY_UNIT_ID");
        result.Message.ShouldContain("invalid characters");
    }

    [Fact]
    public void ValidateMemoryUnitId_Exceeding200Chars_ShouldReturnError()
    {
        string longId = new('a', 201);
        ErrorResponse? result = CaseValidator.ValidateMemoryUnitId(longId);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMORY_UNIT_ID");
        result.Message.ShouldContain("200");
    }

    [Fact]
    public void ValidateMemoryUnitId_Exactly200Chars_ShouldReturnNull()
    {
        string maxId = new('a', 200);
        ErrorResponse? result = CaseValidator.ValidateMemoryUnitId(maxId);

        result.ShouldBeNull();
    }

    // --- ValidateDeleteMemoryUnit tests ---

    [Fact]
    public void ValidateDeleteMemoryUnit_ValidInput_ShouldReturnNull()
    {
        ErrorResponse? result = CaseValidator.ValidateDeleteMemoryUnit("tenant-1", "case-001", "mu-001");

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateDeleteMemoryUnit_InvalidTenantId_ShouldReturnError()
    {
        ErrorResponse? result = CaseValidator.ValidateDeleteMemoryUnit("bad!", "case-001", "mu-001");

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Fact]
    public void ValidateDeleteMemoryUnit_InvalidCaseId_ShouldReturnError()
    {
        ErrorResponse? result = CaseValidator.ValidateDeleteMemoryUnit("tenant-1", "case:bad", "mu-001");

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_CASE_ID");
    }

    [Fact]
    public void ValidateDeleteMemoryUnit_InvalidMemoryUnitId_ShouldReturnError()
    {
        ErrorResponse? result = CaseValidator.ValidateDeleteMemoryUnit("tenant-1", "case-001", "mu:bad");

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMORY_UNIT_ID");
    }

    // --- ValidateCreateAnnotation tests ---

    [Fact]
    public void ValidateCreateAnnotation_ValidInput_ShouldReturnNull()
    {
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "This is an annotation", "user@example.com");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateCreateAnnotation_WithValidAnnotationType_ShouldReturnNull()
    {
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "This is a correction", "user@example.com", "correction");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateCreateAnnotation_InvalidTenantId_ShouldReturnError()
    {
        var input = new CreateAnnotationInput("bad!", "case-001", "mu-001", "content", "user@example.com");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("bad!", "case-001", "mu-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_TENANT_ID");
    }

    [Fact]
    public void ValidateCreateAnnotation_InvalidCaseId_ShouldReturnError()
    {
        var input = new CreateAnnotationInput("tenant-1", "case:bad", "mu-001", "content", "user@example.com");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case:bad", "mu-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_CASE_ID");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateCreateAnnotation_EmptyTargetMuId_ShouldReturnError(string? targetMuId)
    {
        var input = new CreateAnnotationInput("tenant-1", "case-001", targetMuId!, "content", "user@example.com");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", targetMuId!, input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_MEMORY_UNIT_ID");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateCreateAnnotation_EmptyContent_ShouldReturnError(string content)
    {
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", content, "user@example.com");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_ANNOTATION_CONTENT");
    }

    [Fact]
    public void ValidateCreateAnnotation_ContentExceeding50000Chars_ShouldReturnError()
    {
        string longContent = new('A', 50001);
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", longContent, "user@example.com");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_ANNOTATION_CONTENT");
    }

    [Fact]
    public void ValidateCreateAnnotation_ContentExactly50000Chars_ShouldBeValid()
    {
        string maxContent = new('A', 50000);
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", maxContent, "user@example.com");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateCreateAnnotation_EmptyIngestedBy_ShouldReturnError(string ingestedBy)
    {
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "content", ingestedBy);
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_INGESTED_BY");
    }

    [Fact]
    public void ValidateCreateAnnotation_IngestedByExceeding200Chars_ShouldReturnError()
    {
        string longIngestedBy = new('u', 201);
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "content", longIngestedBy);
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_INGESTED_BY");
    }

    [Fact]
    public void ValidateCreateAnnotation_InvalidAnnotationType_ShouldReturnError()
    {
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "content", "user@example.com", "unknown-type");
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("INVALID_ANNOTATION_TYPE");
    }

    [Theory]
    [InlineData("correction")]
    [InlineData("clarification")]
    [InlineData("enrichment")]
    [InlineData("Correction")]
    [InlineData("CLARIFICATION")]
    public void ValidateCreateAnnotation_AllowedAnnotationTypes_ShouldReturnNull(string annotationType)
    {
        var input = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "content", "user@example.com", annotationType);
        ErrorResponse? result = CaseValidator.ValidateCreateAnnotation("tenant-1", "case-001", "mu-001", input);

        result.ShouldBeNull();
    }

    // --- ValidateNotNestedAnnotation tests ---

    [Fact]
    public void ValidateNotNestedAnnotation_NullMetadata_ShouldReturnNull()
    {
        ErrorResponse? result = CaseValidator.ValidateNotNestedAnnotation(null);

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateNotNestedAnnotation_EmptyMetadata_ShouldReturnNull()
    {
        var metadata = new Dictionary<string, MetadataField>();
        ErrorResponse? result = CaseValidator.ValidateNotNestedAnnotation(metadata);

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateNotNestedAnnotation_MetadataWithoutAnnotationTarget_ShouldReturnNull()
    {
        var metadata = new Dictionary<string, MetadataField>
        {
            ["some_key"] = new MetadataField("value", MetadataOrigin.Human, 1.0f),
        };
        ErrorResponse? result = CaseValidator.ValidateNotNestedAnnotation(metadata);

        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateNotNestedAnnotation_MetadataWithAnnotationTarget_ShouldReturnError()
    {
        var metadata = new Dictionary<string, MetadataField>
        {
            ["_system.annotation_target"] = new MetadataField("mu-original", MetadataOrigin.Human, 1.0f),
        };
        ErrorResponse? result = CaseValidator.ValidateNotNestedAnnotation(metadata);

        result.ShouldNotBeNull();
        result!.Code.ShouldBe("NESTED_ANNOTATION_NOT_ALLOWED");
    }
}
