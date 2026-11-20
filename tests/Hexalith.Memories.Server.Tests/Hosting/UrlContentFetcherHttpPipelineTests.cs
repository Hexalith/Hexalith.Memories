// <copyright file="UrlContentFetcherHttpPipelineTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tests.Ingestion;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

[Collection(EmbeddingProviderDefaultsStateCollection.Name)]
public class UrlContentFetcherHttpPipelineTests
{
    private const string ResilienceControlClientName = "resilience-control";

    [Fact]
    public void AddMemoriesServerServices_UrlFetcherPipeline_HasNoHandlerLevelResilience()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        builder.Services.AddHttpClient(ResilienceControlClientName);
        builder.AddMemoriesServerServices();
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IHttpMessageHandlerFactory handlerFactory = provider.GetRequiredService<IHttpMessageHandlerFactory>();

        string[] controlHandlerTypes = GetHandlerTypeNames(handlerFactory.CreateHandler(ResilienceControlClientName));
        string[] urlFetcherHandlerTypes = GetHandlerTypeNames(handlerFactory.CreateHandler(UrlContentFetcher.HttpClientName));

        controlHandlerTypes.Any(IsResilienceHandler).ShouldBeTrue(
            "The control client must prove AddServiceDefaults installed the standard resilience handler.");
        urlFetcherHandlerTypes.Any(IsResilienceHandler).ShouldBeFalse(
            "URL retries belong to the durable workflow; a handler-level retry would recreate Story 26.7's nested retry defect.");
    }

    [Theory]
    [InlineData(200, 6106)]
    [InlineData(404, 6107)]
    public async Task SignedUrlReachesTransportWithoutAppearingInFetchLogs(int status, int terminalEvent)
    {
        string? requestedUrl = null;
        WebApplicationBuilder upstreamBuilder = WebApplication.CreateBuilder();
        upstreamBuilder.WebHost.UseTestServer();
        upstreamBuilder.Logging.ClearProviders();
        await using WebApplication upstream = upstreamBuilder.Build();
        upstream.Run(async context =>
        {
            requestedUrl = context.Request.GetEncodedUrl();
            context.Response.StatusCode = status;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("synthetic body");
        });
        await upstream.StartAsync(TestContext.Current.CancellationToken);

        using CapturingAuditLoggerProvider logs = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddServiceDefaults(configureRedisInstrumentation: false);
        builder.AddMemoriesServerServices();
        builder.Logging.AddProvider(logs).SetMinimumLevel(LogLevel.Trace);
        builder.Services.AddHttpClient(UrlContentFetcher.HttpClientName).ConfigurePrimaryHttpMessageHandler(upstream.GetTestServer().CreateHandler);
        await using ServiceProvider provider = builder.Services.BuildServiceProvider();
        FetchUrlActivity activity = new(provider.GetRequiredService<IUrlContentFetcher>(),
            provider.GetRequiredService<PerTenantConcurrencyGate>(), provider.GetRequiredService<ILogger<FetchUrlActivity>>());
        var input = new FetchUrlInput(SignedUrlLogAssertions.Url, "unit-fetch", "tenant-a");
        var context = Substitute.For<WorkflowActivityContext>();
        if (status == 200)
        {
            (await activity.RunAsync(context, input)).HttpStatusCode.ShouldBe(status);
        }
        else
        {
            (await Should.ThrowAsync<UrlFetchException>(() => activity.RunAsync(context, input))).HttpStatusCode.ShouldBe(status);
        }

        requestedUrl.ShouldBe(SignedUrlLogAssertions.Url);
        logs.Captures.ShouldContain(log => log.EventId == 6105 && log.Message.Contains(input.MemoryUnitId));
        logs.Captures.ShouldContain(log => log.EventId == terminalEvent && log.Message.Contains(input.MemoryUnitId));
        logs.Captures.ShouldProtectSource();
    }

    private static string[] GetHandlerTypeNames(HttpMessageHandler handler)
    {
        List<string> handlerTypeNames = [];
        HttpMessageHandler current = handler;
        while (true)
        {
            handlerTypeNames.Add(current.GetType().FullName ?? current.GetType().Name);
            if (current is not DelegatingHandler { InnerHandler: not null } delegatingHandler)
            {
                return [.. handlerTypeNames];
            }

            current = delegatingHandler.InnerHandler;
        }
    }

    private static bool IsResilienceHandler(string handlerTypeName)
        => handlerTypeName.Contains("ResilienceHandler", StringComparison.Ordinal);
}
