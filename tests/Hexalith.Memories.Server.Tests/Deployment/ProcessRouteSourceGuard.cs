// <copyright file="ProcessRouteSourceGuard.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Finds local declarations of routes with a literal process segment.</summary>
internal static class ProcessRouteSourceGuard
{
    private static readonly Regex RazorPage = new(
        "^\\s*@page\\s+\"([^\"]+)\"", RegexOptions.CultureInvariant);

    /// <summary>Returns the file and line of matching declarations from one production project.</summary>
    internal static IEnumerable<(string Path, int Line)> FindDeclarations(IEnumerable<(string Path, string Source)> files)
    {
        var sources = files.ToArray();
        SyntaxTree[] syntaxTrees = sources.Where(file => Path.GetExtension(file.Path) == ".cs")
            .Select(file => CSharpSyntaxTree.ParseText(file.Source, path: file.Path))
            .ToArray();

        // Constant values and aliases bind across files in the same project. No emit or package
        // restore is needed because endpoint methods themselves only need to be recognized syntactically.
        CSharpCompilation compilation = CSharpCompilation.Create(
            nameof(ProcessRouteSourceGuard), syntaxTrees, [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        foreach (SyntaxTree tree in syntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            IEnumerable<int> lines = tree.GetRoot().DescendantNodes().SelectMany(node => node switch
                {
                    InvocationExpressionSyntax call => GetRouteArguments(call),
                    AttributeSyntax attribute => GetAttributeArguments(attribute),
                    VariableDeclaratorSyntax variable => GetConstantPath(variable, model),
                    _ => [],
                })
                .Where(expression => ContainsProcessTemplate(expression, model))
                .Select(expression => expression.GetLocation().GetLineSpan().StartLinePosition.Line + 1)
                .Distinct();
            foreach (int line in lines)
            {
                yield return (tree.FilePath, line);
            }
        }

        foreach ((string path, string source) in sources.Where(file => Path.GetExtension(file.Path) == ".razor"))
        {
            foreach ((Match match, int line) in source.Split('\n').Select((text, index) => (RazorPage.Match(text), index + 1)))
            {
                if (match.Success && HasProcessSegment(match.Groups[1].Value))
                {
                    yield return (path, line);
                }
            }
        }
    }

    /// <summary>Checks a route pattern for the reserved segment, ignoring case.</summary>
    internal static bool HasProcessSegment(string? pattern)
        => pattern?.Split('/').Contains("process", StringComparer.OrdinalIgnoreCase) == true;

    private static IEnumerable<ExpressionSyntax> GetRouteArguments(InvocationExpressionSyntax call)
    {
        SimpleNameSyntax? name = call.Expression is MemberAccessExpressionSyntax member
            ? member.Name
            : call.Expression as SimpleNameSyntax;
        string? method = name?.Identifier.ValueText;
        if (method is null || !method.StartsWith("Map", StringComparison.Ordinal))
        {
            return [];
        }

        SeparatedSyntaxList<ArgumentSyntax> arguments = call.ArgumentList.Arguments;
        ArgumentSyntax? template = arguments.FirstOrDefault(arg => arg.NameColon?.Name.Identifier.ValueText is
            "pattern" or "routePattern" or "route" or "template");
        int index = method switch
        {
            "MapControllerRoute" => 1,
            "MapAreaControllerRoute" => 2,
            _ => 0,
        };
        template ??= index < arguments.Count && arguments[index].NameColon is null ? arguments[index] : null;
        return template is null ? [] : [template.Expression];
    }

    private static IEnumerable<ExpressionSyntax> GetAttributeArguments(AttributeSyntax attribute)
    {
        string name = attribute.Name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => string.Empty,
        };
        return name is "Route" or "RouteAttribute" or "AcceptVerbs" or "AcceptVerbsAttribute" ||
            name is "HttpGet" or "HttpPost" or "HttpPut" or "HttpPatch" or "HttpDelete" or "HttpHead" or "HttpOptions" ||
            name is "HttpGetAttribute" or "HttpPostAttribute" or "HttpPutAttribute" or "HttpPatchAttribute" or
                "HttpDeleteAttribute" or "HttpHeadAttribute" or "HttpOptionsAttribute"
            ? attribute.ArgumentList?.Arguments.Select(arg => arg.Expression) ?? []
            : [];
    }

    private static IEnumerable<ExpressionSyntax> GetConstantPath(VariableDeclaratorSyntax variable, SemanticModel model)
    {
        if (variable.Parent?.Parent is FieldDeclarationSyntax field &&
            field.Modifiers.Any(SyntaxKind.ConstKeyword) &&
            variable.Initializer?.Value is ExpressionSyntax expression &&
            model.GetConstantValue(expression) is { HasValue: true, Value: string constant } &&
            constant.StartsWith('/'))
        {
            return [expression];
        }

        return [];
    }

    private static bool ContainsProcessTemplate(ExpressionSyntax expression, SemanticModel model)
    {
        Optional<object?> constant = model.GetConstantValue(expression);
        if (constant.HasValue)
        {
            return constant.Value is string value && HasProcessSegment(value);
        }

        return expression.DescendantNodesAndSelf().Any(node => node switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
                => HasProcessSegment(literal.Token.ValueText),
            InterpolatedStringTextSyntax text => HasProcessSegment(text.TextToken.ValueText),
            _ => false,
        });
    }
}
