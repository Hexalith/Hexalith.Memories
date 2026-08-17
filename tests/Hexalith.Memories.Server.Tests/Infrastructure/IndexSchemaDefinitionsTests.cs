// <copyright file="IndexSchemaDefinitionsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Infrastructure;

using System.Reflection;

using Hexalith.Memories.Server.Infrastructure;

using NRedisStack.Search;

using Shouldly;

using StackExchange.Redis;

public class IndexSchemaDefinitionsTests
{
    [Fact]
    public void BuildSyntacticKey_ReturnsTenantScopedMemoryUnitHashKey()
        => IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", "mu-1")
            .ShouldBe("tenant-a:mu:mu-1");

    [Fact]
    public void BuildSemanticKey_ReturnsTenantScopedVectorHashKey()
        => IndexSchemaDefinitions.BuildSemanticKey("tenant-a", "mu-1")
            .ShouldBe("tenant-a:vec:mu-1");

    [Fact]
    public void BuildSemanticChunkKey_ReturnsTenantScopedChunkVectorHashKey()
        => IndexSchemaDefinitions.BuildSemanticChunkKey("tenant-a", "mu-1", 3)
            .ShouldBe("tenant-a:vec:mu-1:3");

    [Fact]
    public void BuildSemanticChunkKeyPattern_GlobMetacharacters_EscapesIdentifierAsLiteral()
        => IndexSchemaDefinitions.BuildSemanticChunkKeyPattern("tenant-a", @"mu*?[x]\")
            .ShouldBe(@"tenant-a:vec:mu\*\?\[x\]\\:*");

    [Fact]
    public void BuildNaturalLanguageSemanticKey_ReturnsDisjointTenantScopedVectorHashKey()
        => IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1")
            .ShouldBe("tenant-a:vecnl:mu-1");

    [Fact]
    public void BuildLegacyNaturalLanguageSemanticKey_ReturnsNestedMigrationOnlyHashKey()
        => IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")
            .ShouldBe("tenant-a:vec:nl:mu-1");

    [Fact]
    public void BuildKeys_NullMemoryUnitId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", null!));
        Should.Throw<ArgumentNullException>(() => IndexSchemaDefinitions.BuildSemanticKey("tenant-a", null!));
        Should.Throw<ArgumentNullException>(() => IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", null!));
        Should.Throw<ArgumentNullException>(() => IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", null!));
    }

    [Fact]
    public void BuildKeys_WhitespaceMemoryUnitId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.BuildSyntacticKey("tenant-a", " "));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.BuildSemanticKey("tenant-a", " "));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", " "));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", " "));
    }

    [Fact]
    public void BuildKeys_WhitespaceTenantId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.BuildSyntacticKey(" ", "mu-1"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.BuildSemanticKey(" ", "mu-1"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(" ", "mu-1"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(" ", "mu-1"));
    }

    [Fact]
    public void IndexAndPrefixHelpers_InvalidTenantId_ThrowArgumentException()
    {
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.GetSyntacticIndexName("bad tenant"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.GetSemanticIndexName("bad tenant"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("bad tenant"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.GetSyntacticKeyPrefix("bad tenant"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.GetSemanticKeyPrefix("bad tenant"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("bad tenant"));
        Should.Throw<ArgumentException>(() => IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix("bad tenant"));
    }

    [Fact]
    public void GetNaturalLanguageSemanticIndexName_AppendsSuffix()
        => IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")
            .ShouldBe("tenant-a:memories:vec:nl");

    [Fact]
    public void ActiveAliasHelpers_ReturnTenantScopedSearchAliases()
    {
        IndexSchemaDefinitions.GetSemanticActiveAliasName("tenant-a")
            .ShouldBe("tenant-a:memories:vec:active");
        IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName("tenant-a")
            .ShouldBe("tenant-a:memories:vec:nl:active");
    }

    [Fact]
    public void StagingHelpers_ReturnVersionedTenantScopedNames()
    {
        IndexSchemaDefinitions.GetSemanticStagingIndexName("tenant-a", "run-1")
            .ShouldBe("tenant-a:memories:vec:staging:run-1");
        IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName("tenant-a", "run-1")
            .ShouldBe("tenant-a:memories:vec:nl:staging:run-1");
        IndexSchemaDefinitions.BuildSemanticStagingKey("tenant-a", "run-1", "mu-1")
            .ShouldBe("tenant-a:vec:staging:run-1:mu-1");
        IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey("tenant-a", "run-1", "mu-1")
            .ShouldBe("tenant-a:vecnl:staging:run-1:mu-1");
    }

    [Fact]
    public void TryParseStagingVersion_ExactCanonicalKeys_ReturnVersions()
    {
        bool rawParsed = IndexSchemaDefinitions.TryParseSemanticStagingVersion(
            "tenant-a",
            IndexSchemaDefinitions.BuildSemanticStagingKey("tenant-a", "run-1", "mu-1"),
            "mu-1",
            out string rawVersion);
        bool naturalLanguageParsed = IndexSchemaDefinitions.TryParseNaturalLanguageSemanticStagingVersion(
            "tenant-a",
            IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey("tenant-a", "run-2", "mu-2"),
            "mu-2",
            out string naturalLanguageVersion);

        rawParsed.ShouldBeTrue();
        rawVersion.ShouldBe("run-1");
        naturalLanguageParsed.ShouldBeTrue();
        naturalLanguageVersion.ShouldBe("run-2");
    }

    [Fact]
    public void TryParseStagingVersion_ActiveReservedLookingOpaqueId_DoesNotGuessStaging()
    {
        const string MemoryUnitId = "staging:run-1:mu-1";

        bool parsed = IndexSchemaDefinitions.TryParseSemanticStagingVersion(
            "tenant-a",
            IndexSchemaDefinitions.BuildSemanticKey("tenant-a", MemoryUnitId),
            MemoryUnitId,
            out string version);

        parsed.ShouldBeFalse();
        version.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("tenant-a:vec:mu-1", "mu-1", false, null, null, null, nameof(SemanticKeyFamily.ActiveRawBase))]
    [InlineData("tenant-a:vec:mu-1:7", "mu-1", false, "7", "0", "12", nameof(SemanticKeyFamily.ActiveRawChunk))]
    [InlineData("tenant-a:vecnl:mu-1", "mu-1", true, null, null, null, nameof(SemanticKeyFamily.ActiveNaturalLanguage))]
    [InlineData("tenant-a:vec:staging:run-1:mu-1", "mu-1", false, null, null, null, nameof(SemanticKeyFamily.RawStaging))]
    [InlineData("tenant-a:vecnl:staging:run-1:mu-1", "mu-1", true, null, null, null, nameof(SemanticKeyFamily.NaturalLanguageStaging))]
    [InlineData("tenant-a:vec:nl:mu-1", "mu-1", true, null, null, null, nameof(SemanticKeyFamily.LegacyNaturalLanguage))]
    public void SemanticFamilyClassifier_CurrentFamilyShape_ReturnsExactlyOneRegisteredFamily(
        string key,
        string memoryUnitId,
        bool hasNaturalLanguageDescription,
        string? chunkSequence,
        string? chunkStartOffset,
        string? chunkEndOffset,
        string expectedName)
    {
        SemanticKeyFamily expected = Enum.Parse<SemanticKeyFamily>(expectedName);
        SemanticKeyFamily actual = ClassifySemanticKey(
            key,
            memoryUnitId,
            hasNaturalLanguageDescription,
            chunkSequence,
            chunkStartOffset,
            chunkEndOffset);

        actual.ShouldBe(expected);
        SemanticKeyFamilyClassifier.RegisteredFamilies.ShouldContain(actual);
    }

    [Theory]
    [InlineData("tenant-a:vec:staging:run-1:mu-1", "staging:run-1:mu-1", false, null, null, null, nameof(SemanticKeyFamily.ActiveRawBase))]
    [InlineData("tenant-a:vec:nl:mu-1", "nl:mu-1", false, null, null, null, nameof(SemanticKeyFamily.ActiveRawBase))]
    [InlineData("tenant-a:vec:mu-1:7", "mu-1:7", false, null, null, null, nameof(SemanticKeyFamily.ActiveRawBase))]
    [InlineData("tenant-a:vec:staging:run-1:mu-1:3", "staging:run-1:mu-1", false, "3", "0", "12", nameof(SemanticKeyFamily.ActiveRawChunk))]
    [InlineData("tenant-a:vecnl:staging:run-1:mu-1", "staging:run-1:mu-1", true, null, null, null, nameof(SemanticKeyFamily.ActiveNaturalLanguage))]
    [InlineData("tenant-a:vec:staging:run-1:nl:mu-1:7", "nl:mu-1:7", false, null, null, null, nameof(SemanticKeyFamily.RawStaging))]
    public void SemanticFamilyClassifier_ReservedLookingOpaqueId_UsesStoredIdentityAndCanonicalReconstruction(
        string key,
        string memoryUnitId,
        bool hasNaturalLanguageDescription,
        string? chunkSequence,
        string? chunkStartOffset,
        string? chunkEndOffset,
        string expectedName)
        => ClassifySemanticKey(
                key,
                memoryUnitId,
                hasNaturalLanguageDescription,
                chunkSequence,
                chunkStartOffset,
                chunkEndOffset)
            .ShouldBe(Enum.Parse<SemanticKeyFamily>(expectedName));

    [Theory]
    [InlineData("tenant-a:vec:future:run-1:mu-1", "mu-1", false, null, null, null)]
    [InlineData("tenant-a:vec:mu-1", "mu-1", true, null, null, null)]
    [InlineData("tenant-a:vec:mu-1:7", "mu-1", false, "7", null, "12")]
    [InlineData("tenant-a:vec:mu-1:7", "mu-1", false, "7", "12", "12")]
    [InlineData("tenant-a:vec:mu-1:7", "mu-1", false, "007", "0", "12")]
    [InlineData("tenant-a:vec:mu-1", "different-id", false, null, null, null)]
    public void SemanticFamilyClassifier_UnregisteredOrContradictoryShape_ReturnsUnknown(
        string key,
        string memoryUnitId,
        bool hasNaturalLanguageDescription,
        string? chunkSequence,
        string? chunkStartOffset,
        string? chunkEndOffset)
        => ClassifySemanticKey(
                key,
                memoryUnitId,
                hasNaturalLanguageDescription,
                chunkSequence,
                chunkStartOffset,
                chunkEndOffset)
            .ShouldBe(SemanticKeyFamily.Unknown);

    [Fact]
    public void SemanticFamilyClassifier_RegistryCoversEveryExplicitFamilyAndOnlyActiveFamiliesPermitMarkerEvidence()
    {
        SemanticKeyFamily[] explicitFamilies = Enum.GetValues<SemanticKeyFamily>()
            .Except([SemanticKeyFamily.Unknown, SemanticKeyFamily.Ambiguous])
            .ToArray();

        SemanticKeyFamilyClassifier.RegisteredFamilies.ShouldBe(explicitFamilies, ignoreOrder: true);
        explicitFamilies
            .Where(SemanticKeyFamilyClassifier.IsActiveMarkerEvidenceFamily)
            .ShouldBe(
                [
                    SemanticKeyFamily.ActiveRawBase,
                    SemanticKeyFamily.ActiveRawChunk,
                    SemanticKeyFamily.ActiveNaturalLanguage,
                ],
                ignoreOrder: true);
    }

    [Fact]
    public void SemanticFamilyClassifier_SchemaNamespaceRegistryCoversEveryDeclaredSemanticKeyPrefixConstant()
    {
        string[] declaredNamespaceConstants = typeof(IndexSchemaDefinitions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral
                && field.FieldType == typeof(string)
                && field.Name.Contains("SemanticKeyPrefixSuffix", StringComparison.Ordinal))
            .Select(field => field.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        string[] registeredNamespaceConstants = IndexSchemaDefinitions.SemanticKeyNamespaceRegistrations
            .Keys
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        SemanticKeyFamily[] namespaceFamilies = IndexSchemaDefinitions.SemanticKeyNamespaceRegistrations
            .Values
            .SelectMany(families => families)
            .Distinct()
            .ToArray();

        registeredNamespaceConstants.ShouldBe(declaredNamespaceConstants);
        namespaceFamilies.ShouldBe(
            SemanticKeyFamilyClassifier.RegisteredFamilies.ToArray(),
            ignoreOrder: true);
    }

    [Fact]
    public void SemanticFamilyClassifier_UnregisteredFutureEnumValue_FailsClosed()
        => Should.Throw<ArgumentOutOfRangeException>(() =>
            SemanticKeyFamilyClassifier.IsActiveMarkerEvidenceFamily((SemanticKeyFamily)int.MaxValue));

    [Fact]
    public void GetNaturalLanguageSemanticKeyPrefix_AppendsSuffix()
        => IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a")
            .ShouldBe("tenant-a:vecnl:");

    [Fact]
    public void NaturalLanguageKeyPrefix_DoesNotCollideWithSemanticKeyPrefix()
    {
        string raw = IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a");
        string nl = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a");

        nl.ShouldNotStartWith(raw);
        nl.ShouldNotBe(raw);
        (raw + "memory-id").ShouldNotStartWith(nl);
        (nl + "memory-id").ShouldNotStartWith(raw);
    }

    [Fact]
    public void RawSemanticIndexPrefix_DoesNotMatchNaturalLanguageHashesAfterRebuild()
    {
        string rawPrefix = IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a");
        string nlHashKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1");

        // RediSearch FT.CREATE PREFIX uses string-prefix matching; this pins that the rebuilt raw
        // semantic index cannot select NL-only hashes.
        nlHashKey.ShouldNotStartWith(rawPrefix);
    }

    [Fact]
    public void GetLegacyNaturalLanguageSemanticKeyPrefix_AppendsNestedSuffixForMigrationOnly()
        => IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix("tenant-a")
            .ShouldBe("tenant-a:vec:nl:");

    [Fact]
    public void TryParseSyntacticMemoryUnitId_MatchingTenantKey_ReturnsId()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:mu:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeTrue();
        memoryUnitId.ShouldBe("mu-1");
    }

    [Fact]
    public void TryParseSemanticMemoryUnitId_MatchingTenantKey_ReturnsId()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:vec:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeTrue();
        memoryUnitId.ShouldBe("mu-1");
    }

    [Fact]
    public void TryParseSemanticMemoryUnitId_ChunkKey_ReturnsBaseId()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:vec:mu-1:7",
            out string memoryUnitId);

        parsed.ShouldBeTrue();
        memoryUnitId.ShouldBe("mu-1");
    }

    [Fact]
    public void TryParseSemanticChunkKey_ChunkKey_ReturnsBaseIdAndSequence()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSemanticChunkKey(
            "tenant-a",
            (RedisKey)"tenant-a:vec:mu-1:7",
            out string memoryUnitId,
            out int sequence);

        parsed.ShouldBeTrue();
        memoryUnitId.ShouldBe("mu-1");
        sequence.ShouldBe(7);
    }

    [Fact]
    public void TryParseSemanticChunkKey_NaturalLanguageKey_ReturnsFalse()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSemanticChunkKey(
            "tenant-a",
            (RedisKey)"tenant-a:vecnl:mu-1:7",
            out string memoryUnitId,
            out int sequence);

        parsed.ShouldBeFalse();
        memoryUnitId.ShouldBe(string.Empty);
        sequence.ShouldBe(0);
    }

    [Fact]
    public void TryParseSemanticMemoryUnitId_ForeignTenantKey_ReturnsFalse()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-b:vec:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeFalse();
        memoryUnitId.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryParseSemanticMemoryUnitId_LegacyNaturalLanguageKey_ReturnsFalse()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:vec:nl:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeFalse();
        memoryUnitId.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryParseSemanticMemoryUnitId_CurrentNaturalLanguageKey_ReturnsFalse()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:vecnl:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeFalse();
        memoryUnitId.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryParseNaturalLanguageSemanticMemoryUnitId_CurrentKey_ReturnsId()
    {
        bool parsed = IndexSchemaDefinitions.TryParseNaturalLanguageSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:vecnl:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeTrue();
        memoryUnitId.ShouldBe("mu-1");
    }

    [Fact]
    public void TryParseNaturalLanguageSemanticMemoryUnitId_LegacyKey_ReturnsFalse()
    {
        bool parsed = IndexSchemaDefinitions.TryParseNaturalLanguageSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:vec:nl:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeFalse();
        memoryUnitId.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryParseNaturalLanguageSemanticMemoryUnitId_ForeignTenantKey_ReturnsFalse()
    {
        bool parsed = IndexSchemaDefinitions.TryParseNaturalLanguageSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-b:vecnl:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeFalse();
        memoryUnitId.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryParseNaturalLanguageSemanticMemoryUnitId_PrefixOnlyKey_ReturnsFalse()
    {
        bool parsed = IndexSchemaDefinitions.TryParseNaturalLanguageSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:vecnl:",
            out string memoryUnitId);

        parsed.ShouldBeFalse();
        memoryUnitId.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryParseLegacyNaturalLanguageSemanticMemoryUnitId_LegacyKey_ReturnsId()
    {
        bool parsed = IndexSchemaDefinitions.TryParseLegacyNaturalLanguageSemanticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:vec:nl:mu-1",
            out string memoryUnitId);

        parsed.ShouldBeTrue();
        memoryUnitId.ShouldBe("mu-1");
    }

    [Fact]
    public void TryParseSyntacticMemoryUnitId_PrefixOnlyKey_ReturnsFalse()
    {
        bool parsed = IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(
            "tenant-a",
            (RedisKey)"tenant-a:mu:",
            out string memoryUnitId);

        parsed.ShouldBeFalse();
        memoryUnitId.ShouldBe(string.Empty);
    }

    [Fact]
    public void BothSemanticSchemas_HaveIdenticalVectorFieldShape()
    {
        Schema raw = IndexSchemaDefinitions.CreateSemanticSchema(768);
        Schema nl = IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(768);

        Schema.VectorField? rawVector = FindVectorField(raw, "embedding");
        Schema.VectorField? nlVector = FindVectorField(nl, "embedding");

        rawVector.ShouldNotBeNull();
        nlVector.ShouldNotBeNull();

        // Both indexes must use the same vector field algorithm. Dimensions/type/distance metric come
        // from the shared CreateSemanticSchemaCore helper — Risk #5 is verified by the helper existing
        // plus this algorithm assertion.
        rawVector.Algorithm.ShouldBe(nlVector.Algorithm);
    }

    [Fact]
    public void NaturalLanguageSchema_IncludesNaturalLanguageDescriptionTextField()
    {
        Schema nl = IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(128);
        bool hasDescriptionField = nl.Fields.Any(f =>
            f.FieldName.Name == "naturalLanguageDescription" && f.Type == Schema.FieldType.Text);

        hasDescriptionField.ShouldBeTrue();
    }

    [Fact]
    public void SemanticSchema_IncludesCloudEventSubjectTagField()
    {
        Schema raw = IndexSchemaDefinitions.CreateSemanticSchema(128);
        bool hasSubjectField = raw.Fields.Any(f =>
            f.FieldName.Name == "cloudeventSubject" && f.Type == Schema.FieldType.Tag);

        hasSubjectField.ShouldBeTrue();
    }

    [Fact]
    public void SyntacticSchema_IncludesAttributeTagsTagField()
    {
        Schema schema = IndexSchemaDefinitions.CreateSyntacticSchema();
        bool hasAttributeTagsField = schema.Fields.Any(f =>
            f.FieldName.Name == "attributeTags" && f.Type == Schema.FieldType.Tag);

        hasAttributeTagsField.ShouldBeTrue();
    }

    [Fact]
    public void NaturalLanguageSchema_DoesNotIncludeCloudEventSubjectTagField()
    {
        Schema nl = IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(128);
        bool hasSubjectField = nl.Fields.Any(f => f.FieldName.Name == "cloudeventSubject");

        hasSubjectField.ShouldBeFalse();
    }

    [Fact]
    public void GetNaturalLanguageSemanticFieldIdentifiers_MatchesSchema()
    {
        IReadOnlyList<string> identifiers = IndexSchemaDefinitions.GetNaturalLanguageSemanticFieldIdentifiers();

        identifiers.ShouldContain("embedding");
        identifiers.ShouldContain("memoryUnitId");
        identifiers.ShouldContain("caseId");
        identifiers.ShouldContain("naturalLanguageDescription");
    }

    private static Schema.VectorField? FindVectorField(Schema schema, string name)
        => schema.Fields.OfType<Schema.VectorField>().FirstOrDefault(v => v.FieldName.Name == name);

    private static SemanticKeyFamily ClassifySemanticKey(
        string key,
        string memoryUnitId,
        bool hasNaturalLanguageDescription,
        string? chunkSequence,
        string? chunkStartOffset,
        string? chunkEndOffset)
        => SemanticKeyFamilyClassifier.Classify(
            "tenant-a",
            key,
            memoryUnitId,
            chunkSequence is null ? RedisValue.Null : chunkSequence,
            chunkStartOffset is null ? RedisValue.Null : chunkStartOffset,
            chunkEndOffset is null ? RedisValue.Null : chunkEndOffset,
            hasNaturalLanguageDescription);
}
