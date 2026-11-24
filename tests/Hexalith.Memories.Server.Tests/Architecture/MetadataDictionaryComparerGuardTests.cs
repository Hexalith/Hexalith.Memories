// <copyright file="MetadataDictionaryComparerGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Architecture;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using Shouldly;

/// <summary>Guards the ordinal metadata key policy across intermediate server-side dictionary copies.</summary>
public sealed class MetadataDictionaryComparerGuardTests
{
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    [Fact]
    public void ServerMetadataDictionaries_PassAnExplicitOrdinalComparer()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.Memories.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("The guard must run against the Server sources.");
        string server = Path.Combine(directory.FullName, "src", "Hexalith.Memories.Server");
        SyntaxTree[] trees = Directory.EnumerateFiles(server, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(server, path).Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: Path.GetRelativePath(directory.FullName, path)))
            .ToArray();

        CheckConstructors(trees, out int checkedCount).ShouldBeEmpty("Metadata dictionaries must pass StringComparer.Ordinal explicitly.");
        checkedCount.ShouldBeGreaterThan(0, "The guard must bind constructors in the Server sources.");
    }

    [Theory]
    [InlineData("new Dictionary<string, MetadataField>(source)", false)]
    [InlineData("new(source.Count)", false)]
    [InlineData("new MetadataMap(comparer: StringComparer.OrdinalIgnoreCase)", false)]
    [InlineData("new MetadataMap(comparer: StringComparer.Ordinal)", true)]
    [InlineData("new(source, StringComparer.Ordinal)", true)]
    public void Guard_BindsExplicitAndTargetTypedDictionaryConstructors(string expression, bool usesOrdinal)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText($$"""
            using MetadataMap = System.Collections.Generic.Dictionary<string, Hexalith.Memories.Contracts.V1.MetadataField>;
            class Example
            {
                MetadataMap Copy(MetadataMap source) => {{expression}};
            }
            """);
        string[] violations = CheckConstructors([tree], out int checkedCount);

        checkedCount.ShouldBe(1);
        violations.Length.ShouldBe(usesOrdinal ? 0 : 1);
    }

    private static string[] CheckConstructors(SyntaxTree[] trees, out int checkedCount)
    {
        SyntaxTree imports = CSharpSyntaxTree.ParseText("""
            global using System;
            global using System.Collections.Generic;
            global using Hexalith.Memories.Contracts.V1;
            global using Hexalith.Memories.Server.Workflows.Contracts;
            """);
        CSharpCompilation compilation = CSharpCompilation.Create("MetadataComparerGuard", [imports, .. trees], References);
        var metadata = compilation.GetTypeByMetadataName("Hexalith.Memories.Contracts.V1.MetadataField");
        metadata.ShouldNotBeNull();
        var dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2")!;
        var metadataDictionary = dictionary.Construct(compilation.GetSpecialType(SpecialType.System_String), metadata);
        var stringComparer = compilation.GetTypeByMetadataName("System.StringComparer");

        checkedCount = 0;
        List<string> violations = [];
        foreach (SyntaxTree tree in trees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (var expression in tree.GetRoot().DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
            {
                if (!SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(expression).Type, metadataDictionary))
                {
                    continue;
                }

                checkedCount++;
                if (model.GetOperation(expression) is IObjectCreationOperation creation
                    && creation.Arguments.Any(argument => argument is
                    {
                        IsImplicit: false,
                        Parameter.Name: "comparer",
                        Value: IConversionOperation { Operand: IPropertyReferenceOperation { Property.Name: "Ordinal" } property },
                    } && SymbolEqualityComparer.Default.Equals(property.Property.ContainingType, stringComparer)))
                {
                    continue;
                }

                FileLinePositionSpan location = expression.GetLocation().GetLineSpan();
                violations.Add($"{location.Path}:{location.StartLinePosition.Line + 1}");
            }
        }

        return [.. violations];
    }
}
