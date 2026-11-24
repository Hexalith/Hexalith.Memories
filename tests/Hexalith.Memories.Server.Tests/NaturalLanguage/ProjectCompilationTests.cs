// <copyright file="ProjectCompilationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.Diagnostics;

using Hexalith.Memories.TestHelpers.Documentation;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

/// <summary>Checks that the experimental Dapr conversation API can compile only with the Server project's suppression.</summary>
public sealed class ProjectCompilationTests
{
    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task DaprConversationCall_CompilesOnlyWithServerSettings(string configuration)
    {
        string root = TestRepository.Root;
        MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

        SyntaxTree probe = CSharpSyntaxTree.ParseText("""
            using Dapr.AI.Conversation;
            public static class ConversationProbe
            {
                public static object Call(DaprConversationClient client)
                    => client.ConverseAsync([], null!, default);
            }
            """);

        foreach (string project in new[] { "Hexalith.Memories.Server", "Hexalith.Memories.Contracts" })
        {
            string noWarn = await ReadNoWarnAsync(root, project, configuration);
            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithGeneralDiagnosticOption(ReportDiagnostic.Error)
                .WithSpecificDiagnosticOptions(noWarn.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(id => id, _ => ReportDiagnostic.Suppress, StringComparer.Ordinal));
            CSharpCompilation compilation = CSharpCompilation.Create("ConversationProbe", [probe], references, options);
            using var output = new MemoryStream();
            var result = compilation.Emit(output);

            if (project == "Hexalith.Memories.Server")
            {
                result.Success.ShouldBeTrue(string.Join(Environment.NewLine, result.Diagnostics));
                result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "DAPR_CONVERSATION");
            }
            else
            {
                result.Success.ShouldBeFalse();
                result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => diagnostic.Id)
                    .ShouldBe(["DAPR_CONVERSATION"]);
            }
        }
    }

    [Fact]
    public void SharedPropsAndOtherSourceProjects_DoNotDeclareTheServerSuppression()
    {
        string root = TestRepository.Root;
        File.ReadAllText(Path.Combine(root, "Directory.Build.props"))
            .ShouldNotContain("DAPR_CONVERSATION", Case.Sensitive);
        foreach (string project in Directory.EnumerateFiles(
            Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(project) != "Hexalith.Memories.Server.csproj")
            {
                File.ReadAllText(project).ShouldNotContain("DAPR_CONVERSATION", Case.Sensitive, project);
            }
        }
    }

    private static async Task<string> ReadNoWarnAsync(string root, string project, string configuration)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in new[] { "msbuild", Path.Combine("src", project, project + ".csproj"), "-nologo",
            $"-p:Configuration={configuration}", "-getProperty:NoWarn" })
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw;
        }

        string noWarn = await output;
        process.ExitCode.ShouldBe(0, noWarn + await error);
        return noWarn.Trim();
    }
}
