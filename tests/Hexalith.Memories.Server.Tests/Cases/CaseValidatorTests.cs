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
}
