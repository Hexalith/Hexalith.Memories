// <copyright file="OllamaOidcFakeServerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Shouldly;

/// <summary>Tier-2 guards for the Story 13.7 Ollama and OIDC fake endpoints.</summary>
public sealed class OllamaOidcFakeServerTests
{
    [Fact]
    public void Story13_7_AC3_DeterministicVector_ShouldBeStableAndUseExpectedDimensions()
    {
        float[] first = OllamaOidcFakeServer.CreateDeterministicVector(
            OllamaOidcFakeServer.DefaultModel,
            "same input",
            OllamaOidcFakeServer.OllamaDimensions);
        float[] second = OllamaOidcFakeServer.CreateDeterministicVector(
            OllamaOidcFakeServer.DefaultModel,
            "same input",
            OllamaOidcFakeServer.OllamaDimensions);
        float[] different = OllamaOidcFakeServer.CreateDeterministicVector(
            OllamaOidcFakeServer.DefaultModel,
            "different input",
            OllamaOidcFakeServer.OllamaDimensions);

        first.Length.ShouldBe(OllamaOidcFakeServer.OllamaDimensions);
        first.ShouldBe(second, tolerance: 0.000001f);
        first.ShouldNotBe(different);
        first.ShouldContain(value => value != 0f);
        first.ShouldAllBe(value => float.IsFinite(value));
    }

    [Fact]
    public async Task Story13_7_AC4_TokenAndEmbedEndpoints_ShouldValidateShapeWithoutCapturingSecrets()
    {
        string clientSecret = $"example-{Guid.NewGuid():N}";
        await using OllamaOidcFakeServer server = await OllamaOidcFakeServer.StartAsync(clientSecret);

        using HttpClient client = new();
        using HttpResponseMessage tokenResponse = await client.PostAsync(
            server.OidcTokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = OllamaOidcFakeServer.ClientId,
                ["client_secret"] = clientSecret,
                ["scope"] = OllamaOidcFakeServer.Scope,
            }));

        tokenResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument tokenDoc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        string token = tokenDoc.RootElement.GetProperty("access_token").GetString()!;

        using HttpRequestMessage embedRequest = new(HttpMethod.Post, server.GetEmbedUri());
        embedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        embedRequest.Content = JsonContent.Create(
            new { model = OllamaOidcFakeServer.DefaultModel, input = "contract input" });

        using HttpResponseMessage embedResponse = await client.SendAsync(embedRequest);

        embedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument embedDoc = JsonDocument.Parse(await embedResponse.Content.ReadAsStringAsync());
        JsonElement embedding = embedDoc.RootElement.GetProperty("embeddings")[0];
        embedding.GetArrayLength().ShouldBe(OllamaOidcFakeServer.OllamaDimensions);
        server.TokenRequestCount.ShouldBe(1);
        server.EmbedRequestCount.ShouldBe(1);
        string evidence = string.Join(Environment.NewLine, server.RequestEvidence.Select(item => item.ToString()));
        // Assert against the actual values rather than substrings that happen to be absent
        // by coincidence of FakeHttpRequestEvidence.ToString() shape.
        evidence.ShouldNotContain(clientSecret);
        evidence.ShouldNotContain(token);
        evidence.ShouldNotContain($"client_secret={clientSecret}");
        evidence.ShouldNotContain($"Bearer {token}");
    }

    [Fact]
    public async Task Story13_7_AC4_EmbedEndpoint_ShouldRejectWrongPath()
    {
        await using OllamaOidcFakeServer server = await OllamaOidcFakeServer.StartAsync($"example-{Guid.NewGuid():N}");

        using HttpClient client = new();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            server.GetUri("/api/embeddings"),
            new { model = OllamaOidcFakeServer.DefaultModel, input = "wrong path" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        server.EmbedRequestCount.ShouldBe(0);
    }
}
