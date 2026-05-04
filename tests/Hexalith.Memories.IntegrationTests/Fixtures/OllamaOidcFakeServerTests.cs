// <copyright file="OllamaOidcFakeServerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

    [Fact]
    public void Story14_4_AC4_DeleteFixtureOwnedTempDaprDirectory_ShouldRemoveLeafAndConfigOnNormalDispose()
    {
        string fixtureAppId = $"memories-server-it-{Guid.NewGuid():N}";
        string parentDir = Path.Combine(Path.GetTempPath(), "hexalith-memories-dapr", fixtureAppId);
        string configPath = Path.Combine(parentDir, "config.yaml");
        string componentPath = Path.Combine(parentDir, "components", "fake-component.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(componentPath)!);
        File.WriteAllText(configPath, "config: yes");
        File.WriteAllText(componentPath, "kind: Component");

        AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configPath, fixtureAppId);

        File.Exists(configPath).ShouldBeFalse();
        File.Exists(componentPath).ShouldBeFalse();
        Directory.Exists(parentDir).ShouldBeFalse();
        // The shared root %TEMP%/hexalith-memories-dapr must remain untouched.
        Directory.Exists(Path.GetDirectoryName(parentDir)!).ShouldBeTrue();
    }

    [Fact]
    public void Story14_4_AC4_DeleteFixtureOwnedTempDaprDirectory_ShouldRemoveLeafEvenWhenConfigWriteNeverSucceeded()
    {
        string fixtureAppId = $"memories-server-it-{Guid.NewGuid():N}";
        string parentDir = Path.Combine(Path.GetTempPath(), "hexalith-memories-dapr", fixtureAppId);
        Directory.CreateDirectory(parentDir);
        string configPath = Path.Combine(parentDir, "config.yaml");
        // Intentionally do not create config.yaml — simulates initialization failure between
        // Directory.CreateDirectory and File.WriteAllText.

        AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configPath, fixtureAppId);

        Directory.Exists(parentDir).ShouldBeFalse();
    }

    [Fact]
    public void Story14_4_AC4_DeleteFixtureOwnedTempDaprDirectory_ShouldRefuseDeletionWhenLeafNameDoesNotMatchFixtureAppId()
    {
        string realLeaf = $"memories-server-it-{Guid.NewGuid():N}";
        string parentDir = Path.Combine(Path.GetTempPath(), "hexalith-memories-dapr", realLeaf);
        Directory.CreateDirectory(parentDir);
        string configPath = Path.Combine(parentDir, "config.yaml");
        File.WriteAllText(configPath, "config: yes");

        try
        {
            AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(
                configPath,
                fixtureAppId: $"memories-server-it-{Guid.NewGuid():N}");

            File.Exists(configPath).ShouldBeTrue();
            Directory.Exists(parentDir).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(parentDir))
            {
                Directory.Delete(parentDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Story14_4_AC4_DeleteFixtureOwnedTempDaprDirectory_ShouldNoOpWhenConfigPathIsNull()
    {
        // Should never throw; covers the "fixture never created the temp dir" branch.
        AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath: null, fixtureAppId: "irrelevant");
        AspireIngestionPipelineFixture.DeleteFixtureOwnedTempDaprDirectory(configFilePath: string.Empty, fixtureAppId: "irrelevant");
    }

    public enum TokenRejectionScenario
    {
        MissingContentType,
        MissingGrantType,
        MissingClientId,
        MissingClientSecret,
        DuplicateGrantType,
        DuplicateClientId,
        DuplicateClientSecret,
        DuplicateScope,
        WrongGrantType,
        WrongScope,
        MalformedBody,
        WrongHttpMethod,
    }

    [Theory]
    [InlineData(TokenRejectionScenario.MissingContentType)]
    [InlineData(TokenRejectionScenario.MissingGrantType)]
    [InlineData(TokenRejectionScenario.MissingClientId)]
    [InlineData(TokenRejectionScenario.MissingClientSecret)]
    [InlineData(TokenRejectionScenario.DuplicateGrantType)]
    [InlineData(TokenRejectionScenario.DuplicateClientId)]
    [InlineData(TokenRejectionScenario.DuplicateClientSecret)]
    [InlineData(TokenRejectionScenario.DuplicateScope)]
    [InlineData(TokenRejectionScenario.WrongGrantType)]
    [InlineData(TokenRejectionScenario.WrongScope)]
    [InlineData(TokenRejectionScenario.MalformedBody)]
    [InlineData(TokenRejectionScenario.WrongHttpMethod)]
    public async Task Story14_4_AC5_TokenEndpoint_ShouldReject400AndNotCount(TokenRejectionScenario scenario)
    {
        string clientSecret = $"example-{Guid.NewGuid():N}";
        await using OllamaOidcFakeServer server = await OllamaOidcFakeServer.StartAsync(clientSecret);
        using HttpClient client = new();

        using HttpRequestMessage request = BuildTokenRequest(server, clientSecret, scenario);
        using HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        // The fake's per-request counters and evidence are gated on the success path, so a
        // rejected request must never bump them. This guards against a future regression where
        // a branch falls through to AddEvidence/Increment by mistake.
        server.TokenRequestCount.ShouldBe(0);
        server.EmbedRequestCount.ShouldBe(0);
        server.RequestEvidence.ShouldBeEmpty();
    }

    private static HttpRequestMessage BuildTokenRequest(
        OllamaOidcFakeServer server,
        string clientSecret,
        TokenRejectionScenario scenario)
    {
        if (scenario == TokenRejectionScenario.WrongHttpMethod)
        {
            return new HttpRequestMessage(HttpMethod.Get, server.OidcTokenEndpoint);
        }

        if (scenario == TokenRejectionScenario.MissingContentType)
        {
            // Plain text body without application/x-www-form-urlencoded so HasFormContentType is false.
            HttpRequestMessage request = new(HttpMethod.Post, server.OidcTokenEndpoint)
            {
                Content = new StringContent(
                    $"grant_type=client_credentials&client_id={OllamaOidcFakeServer.ClientId}&client_secret={clientSecret}",
                    Encoding.UTF8,
                    "text/plain"),
            };
            return request;
        }

        if (scenario == TokenRejectionScenario.MalformedBody)
        {
            HttpRequestMessage request = new(HttpMethod.Post, server.OidcTokenEndpoint)
            {
                Content = new StringContent("not&a=valid;form==body=??", Encoding.UTF8, "application/x-www-form-urlencoded"),
            };
            return request;
        }

        // Build the form starting from a known-good baseline, then mutate per scenario.
        List<KeyValuePair<string, string>> form =
        [
            new("grant_type", "client_credentials"),
            new("client_id", OllamaOidcFakeServer.ClientId),
            new("client_secret", clientSecret),
        ];

        switch (scenario)
        {
            case TokenRejectionScenario.MissingGrantType:
                form.RemoveAll(kv => kv.Key == "grant_type");
                break;
            case TokenRejectionScenario.MissingClientId:
                form.RemoveAll(kv => kv.Key == "client_id");
                break;
            case TokenRejectionScenario.MissingClientSecret:
                form.RemoveAll(kv => kv.Key == "client_secret");
                break;
            case TokenRejectionScenario.DuplicateGrantType:
                form.Add(new("grant_type", "client_credentials"));
                break;
            case TokenRejectionScenario.DuplicateClientId:
                form.Add(new("client_id", OllamaOidcFakeServer.ClientId));
                break;
            case TokenRejectionScenario.DuplicateClientSecret:
                form.Add(new("client_secret", clientSecret));
                break;
            case TokenRejectionScenario.DuplicateScope:
                form.Add(new("scope", OllamaOidcFakeServer.Scope));
                form.Add(new("scope", OllamaOidcFakeServer.Scope));
                break;
            case TokenRejectionScenario.WrongGrantType:
                form.RemoveAll(kv => kv.Key == "grant_type");
                form.Add(new("grant_type", "password"));
                break;
            case TokenRejectionScenario.WrongScope:
                form.Add(new("scope", "openid profile email"));
                break;
        }

        return new HttpRequestMessage(HttpMethod.Post, server.OidcTokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form),
        };
    }
}
