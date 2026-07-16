// <copyright file="UrlContentFetcherHttpPipelineTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

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
