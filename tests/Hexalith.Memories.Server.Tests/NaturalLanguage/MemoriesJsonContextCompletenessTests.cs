// <copyright file="MemoriesJsonContextCompletenessTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Story 9.2 Task 1.9 / Improvement AD — reflection-based completeness check asserting every
/// candidate public record/class in <c>Hexalith.Memories.Contracts.V1</c> has a corresponding
/// <see cref="JsonSerializableAttribute"/> registration in the source-generated JSON context. Catches AOT
/// serialization-registration omissions class-wide rather than per-type.</summary>
/// <remarks>Types explicitly excluded from the completeness check:
/// <list type="bullet">
///   <item><description>Enums — carry <see cref="JsonConverterAttribute"/>; included transitively by the
///   generator when referenced by a registered parent.</description></item>
///   <item><description>Converters (<c>*Converter</c>).</description></item>
///   <item><description>Static types.</description></item>
///   <item><description>Nested types.</description></item>
///   <item><description>Types decorated with <c>[JsonContextIgnore]</c> (reserved for future opt-out).</description></item>
/// </list></remarks>
public sealed class MemoriesJsonContextCompletenessTests
{
    [Fact]
    public void AllContractTypes_AreRegisteredInJsonSourceGenerationContext()
    {
        Type sourceGenContext = LocateSourceGenerationContext();

        // Read via CustomAttributeData rather than the typed API because JsonSerializableAttribute's
        // Type property is generator-internal in some target frameworks; constructor-argument reflection
        // is stable across framework versions.
        HashSet<Type> registeredTypes = sourceGenContext
            .GetCustomAttributesData()
            .Where(a => a.AttributeType.FullName
                == "System.Text.Json.Serialization.JsonSerializableAttribute")
            .Select(a => a.ConstructorArguments.Count > 0
                ? a.ConstructorArguments[0].Value as Type
                : null)
            .Where(t => t is not null)
            .Select(t => t!)
            .ToHashSet();

        List<string> missing = [];

        foreach (Type candidate in EnumerateContractCandidates())
        {
            if (!registeredTypes.Contains(candidate))
            {
                missing.Add(candidate.FullName ?? candidate.Name);
            }
        }

        missing.ShouldBeEmpty(
            "These public record/class types in Hexalith.Memories.Contracts.V1 are NOT registered in " +
            "MemoriesJsonSourceGenerationContext via [JsonSerializable(typeof(T))]. Add them or, if they " +
            "are not intended for JSON serialization, exclude them from the completeness probe. See " +
            $"Story 9.2 Task 1.9 / Improvement AD.{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    private static Type LocateSourceGenerationContext()
    {
        Assembly contractsAssembly = typeof(MemoriesJsonContext).Assembly;

        Type? sourceGenContext = contractsAssembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "MemoriesJsonSourceGenerationContext");

        sourceGenContext.ShouldNotBeNull(
            "Could not locate MemoriesJsonSourceGenerationContext in the Hexalith.Memories.Contracts " +
            "assembly — the context type may have been renamed or moved.");

        return sourceGenContext;
    }

    private static IEnumerable<Type> EnumerateContractCandidates()
    {
        Assembly contractsAssembly = typeof(MemoriesJsonContext).Assembly;

        foreach (Type type in contractsAssembly.GetTypes())
        {
            if (!IsCandidate(type))
            {
                continue;
            }

            yield return type;
        }
    }

    private static bool IsCandidate(Type type)
    {
        if (type.Namespace != "Hexalith.Memories.Contracts.V1")
        {
            return false;
        }

        if (!type.IsPublic)
        {
            return false;
        }

        if (type.IsNested)
        {
            return false;
        }

        if (type.IsEnum)
        {
            return false;
        }

        if (type.IsInterface)
        {
            return false;
        }

        // Skip static classes (abstract + sealed) — these hold helpers like MemoriesJsonContext itself.
        if (type.IsAbstract && type.IsSealed)
        {
            return false;
        }

        // Skip abstract classes without sealed — base records in exporter/annotation hierarchies are
        // referenced via their concrete subclasses, which ARE registered.
        if (type.IsAbstract)
        {
            return false;
        }

        // Skip generic converter types and anything whose name ends in "Converter" or "Attribute".
        if (type.IsGenericTypeDefinition)
        {
            return false;
        }

        if (type.Name.EndsWith("Converter", StringComparison.Ordinal))
        {
            return false;
        }

        if (type.Name.EndsWith("Attribute", StringComparison.Ordinal))
        {
            return false;
        }

        if (type.Name.EndsWith("Exception", StringComparison.Ordinal))
        {
            return false;
        }

        // Skip records that opt out via type name convention — placeholders, compile-time markers.
        if (type.Name == "Placeholder")
        {
            return false;
        }

        return true;
    }
}
