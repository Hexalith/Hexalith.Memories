namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class AddCaseMemberInputSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        AddCaseMemberInput? deserialized = JsonSerializer.Deserialize<AddCaseMemberInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"memberId\":");
        json.ShouldContain("\"memberType\":");
        json.ShouldNotContain("\"MemberId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"MemberType\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void MemberType_ShouldSerializeAsCamelCaseString()
    {
        var user = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        var role = new AddCaseMemberInput("admin-role", CaseMemberType.Role);

        string userJson = JsonSerializer.Serialize(user, MemoriesJsonContext.Options);
        string roleJson = JsonSerializer.Serialize(role, MemoriesJsonContext.Options);

        userJson.ShouldContain("\"memberType\":\"user\"");
        roleJson.ShouldContain("\"memberType\":\"role\"");
    }

    [Fact]
    public void Deserialized_ShouldMatchOriginalValues()
    {
        var original = new AddCaseMemberInput("user-bob", CaseMemberType.Role);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        AddCaseMemberInput? deserialized = JsonSerializer.Deserialize<AddCaseMemberInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.MemberId.ShouldBe("user-bob");
        deserialized.MemberType.ShouldBe(CaseMemberType.Role);
    }
}
