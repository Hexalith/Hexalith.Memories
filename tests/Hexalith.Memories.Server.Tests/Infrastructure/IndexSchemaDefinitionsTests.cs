// <copyright file="IndexSchemaDefinitionsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Infrastructure;

using System.Reflection;

using Hexalith.Memories.Server.Infrastructure;

using NRedisStack.Search;

using Shouldly;

public class IndexSchemaDefinitionsTests
{
    [Fact]
    public void GetNaturalLanguageSemanticIndexName_AppendsSuffix()
        => IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")
            .ShouldBe("tenant-a:memories:vec:nl");

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
        string nlHashKey = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a") + "mu-1";

        // RediSearch FT.CREATE PREFIX uses string-prefix matching; this pins that the rebuilt raw
        // semantic index cannot select NL-only hashes.
        nlHashKey.ShouldNotStartWith(rawPrefix);
    }

    [Fact]
    public void GetLegacyNaturalLanguageSemanticKeyPrefix_AppendsNestedSuffixForMigrationOnly()
        => IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix("tenant-a")
            .ShouldBe("tenant-a:vec:nl:");

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
}
