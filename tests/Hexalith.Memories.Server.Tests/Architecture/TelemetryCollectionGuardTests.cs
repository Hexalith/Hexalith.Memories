// <copyright file="TelemetryCollectionGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using Hexalith.Memories.TestHelpers.Documentation;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

/// <summary>Protects test listeners and direct emitters for process-wide telemetry from parallel tests.</summary>
public sealed class TelemetryCollectionGuardTests
{
    [Theory]
    [InlineData("Hexalith.Memories.Server.Tests")]
    [InlineData("Hexalith.Memories.AccessTelemetry.Tests")]
    public void SharedTelemetryTests_UseNonparallelCollections(string project)
    {
        string root = Path.Combine(TestRepository.Root, "tests", project);
        ClassDeclarationSyntax[] classes = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj"))
            .SelectMany(path => ParseClasses(File.ReadAllText(path), Path.GetRelativePath(root, path)))
            .ToArray();
        ClassDeclarationSyntax[][] observers = classes.GroupBy(ClassName, StringComparer.Ordinal)
            .Select(group => group.ToArray())
            .Where(UsesTelemetry)
            .ToArray();
        observers.ShouldNotBeEmpty();

        HashSet<string> serialCollections = FindSerialCollections(classes);
        observers.Where(parts => !parts.Any(declaration => HasSerialCollection(declaration, serialCollections)))
            .Select(parts => $"{parts[0].SyntaxTree.FilePath}: {ClassName(parts[0])}")
            .ShouldBeEmpty("Use an xUnit collection with DisableParallelization = true for shared telemetry tests.");
    }

    [Theory]
    [InlineData("", "", false, false)]
    [InlineData("[Collection(Serial.Name)]", "MeterListener listener = new();", true, true)]
    [InlineData("[CollectionAttribute(\"serial\")]", "var listener = new ActivityListener();", true, true)]
    [InlineData("[Collection(\"parallel\")]", "var listener = new System.Diagnostics.Metrics.MeterListener();", true, false)]
    [InlineData("", "AccessTelemetryLifecycleMetrics.Record();", true, false)]
    [InlineData("", "// MeterListener", false, false)]
    [InlineData("[Collection(nameof(Named))]", "TelemetryMetricsRecorder.RecordSearch();", true, true)]
    [InlineData("", "using var counts = new RollingCounterStore();", true, false)]
    [InlineData("", "using var provider = new TracerProvider();", true, false)]
    public void Guard_DetectsSharedTelemetryAndChecksCollectionDefinition(
        string annotation, string code, bool observesTelemetry, bool usesSerialCollection)
    {
        ClassDeclarationSyntax[] classes = ParseClasses($$"""
            [CollectionDefinition(Name, DisableParallelization = true)]
            class Serial { public const string Name = "serial"; }
            [CollectionDefinition(nameof(Named), DisableParallelization = true)]
            class Named { }
            [CollectionDefinition("parallel")]
            class Parallel { }
            {{annotation}}
            class Example
            {
                [Fact] public void Run()
                {
                    {{code}}
                }
            }
            """).ToArray();

        ClassDeclarationSyntax example = classes.Single(declaration => declaration.Identifier.ValueText == "Example");
        UsesTelemetry([example]).ShouldBe(observesTelemetry);
        HasSerialCollection(example, FindSerialCollections(classes)).ShouldBe(usesSerialCollection);
    }

    private static IEnumerable<ClassDeclarationSyntax> ParseClasses(string source, string path = "")
        => CSharpSyntaxTree.ParseText(source, path: path).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

    [Fact]
    public void Guard_CombinesTestMethodsAndListenersAcrossPartialDeclarations()
    {
        ClassDeclarationSyntax[] parts = ParseClasses("""
            namespace Example;
            partial class Case
            {
                [RunnableSkippedFact("Opt-in test")] public void Emit() { }
            }
            partial class Case
            {
                private readonly MeterListener _listener = new();
            }
            """).ToArray();

        parts.Select(ClassName).Distinct(StringComparer.Ordinal).ShouldHaveSingleItem();
        UsesTelemetry(parts).ShouldBeTrue();
    }

    private static string ClassName(ClassDeclarationSyntax declaration)
        => string.Join(".", declaration.AncestorsAndSelf().Reverse().Select(node => node switch
        {
            BaseNamespaceDeclarationSyntax @namespace => @namespace.Name.ToString(),
            TypeDeclarationSyntax type => type.Identifier.ValueText + type.TypeParameterList?.ToString(),
            _ => null,
        }).OfType<string>());

    private static bool UsesTelemetry(ClassDeclarationSyntax[] parts)
        => parts.SelectMany(declaration => declaration.Members).OfType<MethodDeclarationSyntax>().Any(method =>
                Attributes(method.AttributeLists).Any(attribute => AttributeName(attribute) is "Fact" or "Theory" or "RunnableSkippedFact"))
            && parts.Any(ReferencesTelemetry);

    private static bool ReferencesTelemetry(ClassDeclarationSyntax declaration)
        => declaration.DescendantNodes().OfType<IdentifierNameSyntax>().Any(identifier => identifier.Identifier.ValueText is
                "MeterListener" or "ActivityListener" or "TracerProvider" or "RollingCounterStore" or "TelemetryMetricsRecorder"
                or "AccessTelemetryLifecycleMetrics" or "AccessTelemetryLifecycleProcessor" or "AccessTelemetryProcessorStatus"
                or "AccessTelemetryLifecycleActor" or "AccessTelemetryRuntimeGate" or "DaprLifecycleClockEvidenceProvider"
                or "DaprAccessTelemetryStateStore")
            || declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(access =>
                access.Name.Identifier.ValueText == "Instance"
                && access.Expression.GetLastToken().ValueText is "MemoriesMeter" or "MemoriesActivitySource");

    private static HashSet<string> FindSerialCollections(IEnumerable<ClassDeclarationSyntax> classes)
    {
        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (ClassDeclarationSyntax declaration in classes)
        {
            foreach (AttributeSyntax attribute in Attributes(declaration.AttributeLists))
            {
                if (AttributeName(attribute) != "CollectionDefinition" || attribute.ArgumentList is not { } arguments
                    || !arguments.Arguments.Any(argument => argument.NameEquals?.Name.Identifier.ValueText == "DisableParallelization"
                        && argument.Expression.IsKind(SyntaxKind.TrueLiteralExpression)))
                {
                    continue;
                }

                ExpressionSyntax name = arguments.Arguments[0].Expression;
                if (LiteralCollectionName(name) is { } nameValue)
                {
                    result.Add(nameValue);
                }
                else if (name is IdentifierNameSyntax identifier)
                {
                    VariableDeclaratorSyntax? constant = declaration.Members.OfType<FieldDeclarationSyntax>()
                        .Where(field => field.Modifiers.Any(SyntaxKind.ConstKeyword))
                        .SelectMany(field => field.Declaration.Variables)
                        .SingleOrDefault(variable => variable.Identifier.ValueText == identifier.Identifier.ValueText);
                    if (LiteralCollectionName(constant?.Initializer?.Value) is { } value)
                    {
                        result.Add(value);
                        result.Add($"{declaration.Identifier.ValueText}.{identifier.Identifier.ValueText}");
                    }
                }
            }
        }

        return result;
    }

    private static bool HasSerialCollection(ClassDeclarationSyntax declaration, HashSet<string> serialCollections)
        => Attributes(declaration.AttributeLists)
            .Where(attribute => AttributeName(attribute) == "Collection")
            .Any(attribute => attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression switch
            {
                MemberAccessExpressionSyntax member => serialCollections.Contains(
                    $"{member.Expression.GetLastToken().ValueText}.{member.Name.Identifier.ValueText}"),
                var expression => LiteralCollectionName(expression) is { } name && serialCollections.Contains(name),
            });

    private static string? LiteralCollectionName(ExpressionSyntax? expression)
        => expression switch
        {
            LiteralExpressionSyntax literal => literal.Token.ValueText,
            InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } invocation
                => invocation.ArgumentList.Arguments.Single().Expression.GetLastToken().ValueText,
            _ => null,
        };

    private static IEnumerable<AttributeSyntax> Attributes(SyntaxList<AttributeListSyntax> attributes)
        => attributes.SelectMany(list => list.Attributes);

    private static string AttributeName(AttributeSyntax attribute)
    {
        string name = attribute.Name.GetLastToken().ValueText;
        return name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name;
    }
}
