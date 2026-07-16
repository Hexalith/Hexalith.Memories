// <copyright file="CliConsolidationGuardTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Reflection;

using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Output;
using Hexalith.Memories.Cli.Output.Formatters;
using Hexalith.Memories.Cli.Output.Json;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>Guards the consolidated success formatter and Client.Rest transport boundary.</summary>
public sealed class CliConsolidationGuardTests
{
    [Fact]
    public void FormatterTypes_ContainOnlyGenericJsonHumanAndTableFormatters()
    {
        Type genericJsonFormatter = typeof(JsonEnvelopeFormatter<>);
        Type[] unexpectedFormatterTypes = genericJsonFormatter.Assembly
            .GetTypes()
            .Where(static type => type.IsClass && !type.IsAbstract && ImplementsOutputFormatter(type))
            .Where(type => type != genericJsonFormatter)
            .Where(static type => !type.Name.EndsWith("HumanFormatter", StringComparison.Ordinal))
            .Where(static type => !type.Name.EndsWith("TableFormatter", StringComparison.Ordinal))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        unexpectedFormatterTypes.ShouldBeEmpty(
            "JSON success payloads must use JsonEnvelopeFormatter<T>; every other formatter must be a human or table formatter.");
    }

    [Fact]
    public void CliServiceCollection_RegistersExactlyOneJsonFormatterPerSuccessPayload()
    {
        IServiceCollection services = CliServices.BuildCollection();
        using ServiceProvider provider = services.BuildServiceProvider();

        string[] registeredPayloadTypes = services
            .Where(static descriptor => IsOutputFormatterService(descriptor.ServiceType))
            .Select(static descriptor => descriptor.ServiceType.GetGenericArguments()[0].FullName!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        string[] expectedPayloadTypes =
        [
            typeof(IReadOnlyList<TenantSummary>).FullName!,
            typeof(ConfigShowData).FullName!,
            typeof(TelemetrySummary).FullName!,
            typeof(HandlerRegistrationSnapshot).FullName!,
            typeof(HandlerMismatchReport).FullName!,
            typeof(HybridSearchResult).FullName!,
            typeof(SearchResult).FullName!,
            typeof(MemoryUnit).FullName!,
            typeof(MemoryUnitIdLookupResponse).FullName!,
            typeof(ConsistencyInspectionResult).FullName!,
            typeof(ConsistencyVerificationResult).FullName!,
            typeof(ConsistencyRepairResult).FullName!,
            typeof(ConsistencyWorkflowState).FullName!,
            typeof(ConsistencyCommandReceipt).FullName!,
        ];
        Array.Sort(expectedPayloadTypes, StringComparer.Ordinal);
        registeredPayloadTypes.ShouldBe(expectedPayloadTypes);

        AssertFormatterSet<IReadOnlyList<TenantSummary>>(provider, TenantListCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<ConfigShowData>(provider, ConfigShowCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<TelemetrySummary>(provider, StatusTelemetryCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<HandlerRegistrationSnapshot>(provider, HandlersListCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<HandlerMismatchReport>(provider, HandlersMismatchesCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<HybridSearchResult>(provider, SearchQueryCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<SearchResult>(provider, SearchQueryCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<MemoryUnit>(provider, SearchInspectCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<MemoryUnitIdLookupResponse>(provider, SearchLookupCommand.CommandName, OutputFormat.Human, OutputFormat.Json);
        AssertFormatterSet<ConsistencyInspectionResult>(provider, ConsistencyInspectCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<ConsistencyVerificationResult>(provider, ConsistencyVerifyCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<ConsistencyRepairResult>(provider, ConsistencyRepairCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet<ConsistencyWorkflowState>(provider, ConsistencyVerifyCommand.CommandName, OutputFormat.Human, OutputFormat.Json, OutputFormat.Table);
        AssertFormatterSet(
            provider,
            ConsistencyRepairCommand.CommandName,
            OutputFormat.Human,
            OutputFormat.Json,
            OutputFormat.Table,
            selectorValue: new ConsistencyCommandReceipt("acme", "repair-1", "repair", new Uri("https://localhost/repair-1")));
    }

    [Fact]
    public void CommandAndQuickstartSources_DelegateNetworkTransportToMemoriesClient()
    {
        string cliDirectory = Path.Combine(LocateRepoRoot(), "src", "Hexalith.Memories.Cli");
        string[] sourceFiles =
        [
            .. Directory.EnumerateFiles(Path.Combine(cliDirectory, "Commands"), "*.cs", SearchOption.AllDirectories),
            .. Directory.EnumerateFiles(Path.Combine(cliDirectory, "Quickstart"), "*.cs", SearchOption.AllDirectories),
        ];
        string[] forbiddenTransportFragments =
        [
            "HttpClient",
            "HttpRequestMessage",
            "HttpResponseMessage",
            "HttpContent",
            ".SendAsync(",
            "JsonSerializer.Deserialize",
            "ReadFromJsonAsync",
            "GetFromJsonAsync",
            "PostAsJsonAsync",
            "PutAsJsonAsync",
        ];

        List<string> violations = [];
        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);
            foreach (string fragment in forbiddenTransportFragments)
            {
                if (source.Contains(fragment, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(LocateRepoRoot(), sourceFile)} contains {fragment}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "CLI commands and quickstart services must delegate request/response transport and decoding to MemoriesClient.");
        sourceFiles.Select(File.ReadAllText).Any(source => source.Contains("MemoriesClient", StringComparison.Ordinal))
            .ShouldBeTrue("The guard must scan production sources that consume MemoriesClient.");
    }

    private static void AssertFormatterSet<T>(
        IServiceProvider provider,
        string expectedCommand,
        OutputFormat firstFormat,
        OutputFormat secondFormat,
        OutputFormat? thirdFormat = null,
        T selectorValue = null!)
        where T : class
    {
        IOutputFormatter<T>[] formatters = provider
            .GetServices<IOutputFormatter<T>>()
            .ToArray();
        OutputFormat[] expectedFormats = thirdFormat is null
            ? [firstFormat, secondFormat]
            : [firstFormat, secondFormat, thirdFormat.Value];
        formatters.Select(static formatter => formatter.Format).ShouldBe(expectedFormats);

        IOutputFormatter<T> jsonFormatter = formatters.Single(static formatter => formatter.Format == OutputFormat.Json);
        JsonEnvelopeFormatter<T> genericFormatter = jsonFormatter.ShouldBeOfType<JsonEnvelopeFormatter<T>>();
        FieldInfo? selectorField = typeof(JsonEnvelopeFormatter<T>).GetField(
            "_commandSelector",
            BindingFlags.Instance | BindingFlags.NonPublic);
        selectorField.ShouldNotBeNull("The generic JSON formatter command selector must remain inspectable by this DI drift guard.");
        var selector = selectorField.GetValue(genericFormatter).ShouldBeOfType<Func<T, string>>();

        selector(selectorValue).ShouldBe(expectedCommand);
    }

    private static bool ImplementsOutputFormatter(Type type)
        => type.GetInterfaces().Any(IsOutputFormatterService);

    private static bool IsOutputFormatterService(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOutputFormatter<>);

    private static string LocateRepoRoot()
    {
        string? repoRoot = typeof(CliConsolidationGuardTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute => attribute.Key == "RepoRoot")
            ?.Value;

        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            throw new Xunit.Sdk.XunitException("RepoRoot assembly metadata is missing.");
        }

        return Path.GetFullPath(repoRoot);
    }
}
