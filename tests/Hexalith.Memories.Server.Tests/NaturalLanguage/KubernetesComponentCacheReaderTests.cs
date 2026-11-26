// <copyright file="KubernetesComponentCacheReaderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

using Shouldly;

/// <summary>Tests the deployed Conversation configuration read.</summary>
public sealed class KubernetesComponentCacheReaderTests
{
    /// <summary>Exercises live metadata, including values the startup validator cannot safely resolve.</summary>
    [Theory]
    [InlineData(200, "conversation.openai", "{\"name\":\"responseCacheTTL\",\"value\":\"10m\"}", 600)]
    [InlineData(200, "conversation.openai", "{\"name\":\"responseCacheTTL\",\"value\":\"0s\"}", 0)]
    [InlineData(200, "conversation.openai", "{\"name\":\"responseCacheTTL\",\"value\":0}", 0)]
    [InlineData(200, "conversation.openai", "{\"name\":\"responseCacheTTL\",\"value\":30}", 30)]
    [InlineData(200, "conversation.openai", "{\"name\":\"cacheTTL\",\"value\":\"0s\"},{\"name\":\"responseCacheTTL\",\"value\":\"10m\"}", null)]
    [InlineData(200, "conversation.openai", "{\"name\":\"responseCacheTTL\",\"value\":\"0s\"},{\"name\":\"responseCacheTTL\",\"value\":\"10m\"}", null)]
    [InlineData(200, "conversation.openai", "{\"name\":\"responseCacheTTL\",\"secretKeyRef\":{\"name\":\"cache-config\"}}", null)]
    [InlineData(200, "conversation.openai", "{\"name\":\"responseCacheTTL\",\"value\":\"0s\",\"secretKeyRef\":{\"name\":\"cache-config\"}}", null)]
    [InlineData(200, "conversation.echo", "{\"name\":\"responseCacheTTL\",\"value\":\"0s\"}", null)]
    [InlineData(403, "conversation.openai", "", null)]
    [InlineData(404, "conversation.openai", "", null)]
    public async Task ReadComponentCacheTtlAsync_UsesNamedComponentAndScopedToken(
        int statusCode,
        string componentType,
        string metadata,
        int? expectedSeconds)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using WebApplication app = builder.Build();
        app.Run(async context =>
        {
            context.Request.Method.ShouldBe("GET");
            context.Request.Path.Value.ShouldBe("/apis/dapr.io/v1alpha1/namespaces/memories-test/components/llm-openai");
            context.Request.Headers.Authorization.ToString().ShouldBe("Bearer test-only");
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync($$"""
                {"spec":{"type":"{{componentType}}","metadata":[{{metadata}}] } }
                """);
        });
        await app.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = app.GetTestClient();

        TimeSpan? ttl = await KubernetesComponentCacheReader.ReadComponentCacheTtlAsync(client, "memories-test", "llm-openai", "test-only");

        ttl.ShouldBe(expectedSeconds is int seconds ? TimeSpan.FromSeconds(seconds) : (TimeSpan?)null);
    }
}
