namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class CaseMemberSerializationTests
{
    [Fact]
    public void RoundTrip_AllFields_ShouldProduceIdenticalJson()
    {
        CaseMember original = CreateFullMember();
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseMember? deserialized = JsonSerializer.Deserialize<CaseMember>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        CaseMember original = CreateFullMember();
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"memberId\":");
        json.ShouldContain("\"memberType\":");
        json.ShouldContain("\"addedAt\":");
        json.ShouldNotContain("\"MemberId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"MemberType\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"AddedAt\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void MemberType_ShouldSerializeAsCamelCaseString()
    {
        CaseMember user = CreateFullMember() with { MemberType = CaseMemberType.User };
        CaseMember role = CreateFullMember() with { MemberType = CaseMemberType.Role };

        string userJson = JsonSerializer.Serialize(user, MemoriesJsonContext.Options);
        string roleJson = JsonSerializer.Serialize(role, MemoriesJsonContext.Options);

        userJson.ShouldContain("\"memberType\":\"user\"");
        roleJson.ShouldContain("\"memberType\":\"role\"");
    }

    [Fact]
    public void ListOfMembers_ShouldRoundTrip()
    {
        List<CaseMember> members =
        [
            CreateFullMember(),
            CreateFullMember() with { MemberId = "admin-role", MemberType = CaseMemberType.Role },
        ];

        string json1 = JsonSerializer.Serialize(members, MemoriesJsonContext.Options);
        List<CaseMember>? deserialized = JsonSerializer.Deserialize<List<CaseMember>>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
        deserialized.ShouldNotBeNull();
        deserialized.Count.ShouldBe(2);
        deserialized[0].MemberType.ShouldBe(CaseMemberType.User);
        deserialized[1].MemberType.ShouldBe(CaseMemberType.Role);
    }

    [Fact]
    public void Deserialized_ShouldMatchOriginalValues()
    {
        CaseMember original = CreateFullMember();
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseMember? deserialized = JsonSerializer.Deserialize<CaseMember>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.MemberId.ShouldBe("user-alice");
        deserialized.MemberType.ShouldBe(CaseMemberType.User);
        deserialized.AddedAt.ShouldBe(new DateTimeOffset(2026, 4, 12, 10, 0, 0, TimeSpan.Zero));
    }

    private static CaseMember CreateFullMember()
    {
        return new CaseMember(
            "user-alice",
            CaseMemberType.User,
            new DateTimeOffset(2026, 4, 12, 10, 0, 0, TimeSpan.Zero));
    }
}
