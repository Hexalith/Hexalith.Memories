// <copyright file="PublicContractSerializationCoverageTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Collections;
using System.Reflection;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

/// <summary>Reflection guard that keeps public V1 contracts covered by a discoverable JSON round trip.</summary>
public sealed class PublicContractSerializationCoverageTests
{
    public static IEnumerable<object[]> PublicContractTypes()
        => typeof(MemoryUnit).Assembly
            .GetTypes()
            .Where(IsRoundTripContractType)
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .Select(static type => new object[] { type });

    [Theory]
    [MemberData(nameof(PublicContractTypes))]
    public void PublicContractType_ShouldRoundTripThroughMemoriesJsonContext(Type contractType)
    {
        object sample = CreateSample(contractType, []);

        string firstJson = JsonSerializer.Serialize(sample, contractType, MemoriesJsonContext.Options);
        object? deserialized = JsonSerializer.Deserialize(firstJson, contractType, MemoriesJsonContext.Options);
        string secondJson = JsonSerializer.Serialize(deserialized, contractType, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        secondJson.ShouldBe(firstJson);
    }

    private static bool IsRoundTripContractType(Type type)
    {
        if (!type.IsPublic || type.Namespace != "Hexalith.Memories.Contracts.V1")
        {
            return false;
        }

        if (type.IsAbstract || type.ContainsGenericParameters)
        {
            return false;
        }

        if (type == typeof(MemoriesJsonContext)
            || type.Name.StartsWith("CamelCaseStringEnumConverter", StringComparison.Ordinal)
            || type.Name.EndsWith("Validator", StringComparison.Ordinal)
            || type.Name.EndsWith("Defaults", StringComparison.Ordinal)
            || type.Name.EndsWith("Taxonomy", StringComparison.Ordinal))
        {
            return false;
        }

        return type.IsEnum || type.IsClass || type.IsValueType;
    }

    private static object CreateSample(Type type, HashSet<Type> activeTypes)
    {
        Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType == typeof(string))
        {
            return "sample";
        }

        if (effectiveType == typeof(bool))
        {
            return true;
        }

        if (effectiveType == typeof(int))
        {
            return 42;
        }

        if (effectiveType == typeof(long))
        {
            return 42L;
        }

        if (effectiveType == typeof(float))
        {
            return 0.75f;
        }

        if (effectiveType == typeof(double))
        {
            return 0.75d;
        }

        if (effectiveType == typeof(decimal))
        {
            return 0.75m;
        }

        if (effectiveType == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);
        }

        if (effectiveType == typeof(DateTime))
        {
            return new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc);
        }

        if (effectiveType == typeof(Guid))
        {
            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        if (effectiveType.IsEnum)
        {
            Array values = Enum.GetValues(effectiveType);
            return values.Length > 1 ? values.GetValue(1)! : values.GetValue(0)!;
        }

        if (effectiveType.IsArray)
        {
            Type elementType = effectiveType.GetElementType()!;
            Array array = Array.CreateInstance(elementType, 1);
            array.SetValue(CreateSample(elementType, activeTypes), 0);
            return array;
        }

        if (TryCreateDictionary(effectiveType, activeTypes, out object? dictionary))
        {
            return dictionary!;
        }

        if (TryCreateEnumerable(effectiveType, activeTypes, out object? enumerable))
        {
            return enumerable!;
        }

        if (!activeTypes.Add(effectiveType))
        {
            // Recursion guard. Returning `null!` for a non-nullable reference parameter would feed
            // through to the parent constructor and surface as an opaque ArgumentNullException /
            // reflection-invocation error rather than a clear "this contract is recursive and
            // needs explicit handling" signal. Value types still get a default sample so cycles
            // through value-type sub-graphs do not block round-trip coverage on unrelated types.
            if (effectiveType.IsValueType)
            {
                return Activator.CreateInstance(effectiveType)!;
            }

            throw new NotSupportedException(
                $"Public V1 contract type '{effectiveType.FullName}' has a self-referential or mutually-recursive shape. "
                + "Add an explicit exclusion or extend `CreateSample` with a depth-bounded sentinel for this type.");
        }

        try
        {
            ConstructorInfo? constructor = effectiveType
                .GetConstructors()
                .OrderByDescending(static c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor is null)
            {
                return Activator.CreateInstance(effectiveType)!;
            }

            object?[] arguments = constructor
                .GetParameters()
                .Select(parameter => CreateSample(parameter.ParameterType, activeTypes))
                .ToArray();

            return constructor.Invoke(arguments);
        }
        finally
        {
            _ = activeTypes.Remove(effectiveType);
        }
    }

    private static bool TryCreateDictionary(Type type, HashSet<Type> activeTypes, out object? dictionary)
    {
        Type? dictionaryInterface = GetGenericInterface(type, typeof(IDictionary<,>))
            ?? GetGenericInterface(type, typeof(IReadOnlyDictionary<,>));

        if (dictionaryInterface is null)
        {
            dictionary = null;
            return false;
        }

        Type[] arguments = dictionaryInterface.GetGenericArguments();
        Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(arguments);
        IDictionary result = (IDictionary)Activator.CreateInstance(dictionaryType)!;
        result.Add(CreateSample(arguments[0], activeTypes), CreateSample(arguments[1], activeTypes));
        dictionary = result;
        return true;
    }

    private static bool TryCreateEnumerable(Type type, HashSet<Type> activeTypes, out object? enumerable)
    {
        if (type == typeof(string))
        {
            enumerable = null;
            return false;
        }

        Type? enumerableInterface = GetGenericInterface(type, typeof(IEnumerable<>));
        if (enumerableInterface is null)
        {
            enumerable = null;
            return false;
        }

        Type elementType = enumerableInterface.GetGenericArguments()[0];
        Type listType = typeof(List<>).MakeGenericType(elementType);
        IList list = (IList)Activator.CreateInstance(listType)!;
        list.Add(CreateSample(elementType, activeTypes));
        enumerable = list;
        return true;
    }

    private static Type? GetGenericInterface(Type type, Type genericTypeDefinition)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
        {
            return type;
        }

        return type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericTypeDefinition);
    }
}
