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
}
