// <copyright file="OpenBaoInitializerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Net;
using System.Text.Json;

using Hexalith.Memories.AppHost;

using Shouldly;

/// <summary>HTTP-contract tests for the AppHost-owned OpenBao initialization state machine.</summary>
public sealed class OpenBaoInitializerTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:18200");

    [Fact]
    public async Task InitializeAsync_UninitializedOpenBaoUsesExactIsolatedBootstrapContract()
    {
        var handler = new OpenBaoRecordingHandler();
        using var client = new HttpClient(handler);
        var initializer = new OpenBaoInitializer(client);
        OpenBaoSeedInputs seeds = OpenBaoSeedInputs.Create(
            "{\"provider-secret\":\"runtime-secret-value\"}",
            "{\"access-telemetry-marker-key\":\"marker-secret-value\",\"access-telemetry-clock-key\":\"clock-secret-value\"}");

        OpenBaoInitializationResult result = await initializer
            .InitializeAsync(Endpoint, seeds, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.RuntimeToken.ShouldBe("runtime-test-token");
        result.AccessTelemetryToken.ShouldBe("access-test-token");

        OpenBaoCapturedRequest initialize = Single(handler, HttpMethod.Post, "/v1/sys/init");
        using (JsonDocument document = JsonDocument.Parse(initialize.Body))
        {
            document.RootElement.EnumerateObject().Select(property => property.Name)
                .ShouldBe(["secret_shares", "secret_threshold"], ignoreOrder: true);
            document.RootElement.GetProperty("secret_shares").GetInt32().ShouldBe(1);
            document.RootElement.GetProperty("secret_threshold").GetInt32().ShouldBe(1);
            initialize.Body.ShouldNotContain("stored_shares", Case.Sensitive);
        }

        OpenBaoCapturedRequest mount = Single(handler, HttpMethod.Post, "/v1/sys/mounts/secret");
        mount.Token.ShouldBe("root-test-token");
        mount.Body.ShouldContain("\"type\":\"kv\"");
        mount.Body.ShouldContain("\"version\":\"2\"");

        OpenBaoCapturedRequest runtimePolicy = Single(handler, HttpMethod.Put, "/v1/sys/policies/acl/memories-runtime-read");
        string runtimePolicyText = PolicyText(runtimePolicy);
        runtimePolicyText.ShouldContain("secret/data/hexalith/memories/runtime/*");
        runtimePolicyText.ShouldContain("capabilities = [\"read\"]");
        runtimePolicyText.ShouldNotContain("list", Case.Insensitive);
        runtimePolicyText.ShouldNotContain("create", Case.Insensitive);
        runtimePolicyText.ShouldNotContain("update", Case.Insensitive);
        runtimePolicyText.ShouldNotContain("delete", Case.Insensitive);
        runtimePolicyText.ShouldNotContain("sudo", Case.Insensitive);

        OpenBaoCapturedRequest accessPolicy = Single(handler, HttpMethod.Put, "/v1/sys/policies/acl/memories-access-telemetry-read");
        string accessPolicyText = PolicyText(accessPolicy);
        accessPolicyText.ShouldContain("secret/data/hexalith/memories/access-telemetry/*");
        accessPolicyText.ShouldContain("capabilities = [\"read\"]");

        AssertSeed(handler, "/v1/secret/data/hexalith/memories/runtime/provider-secret", "provider-secret");
        AssertSeed(handler, "/v1/secret/data/hexalith/memories/access-telemetry/access-telemetry-marker-key", "access-telemetry-marker-key");
        AssertSeed(handler, "/v1/secret/data/hexalith/memories/access-telemetry/access-telemetry-clock-key", "signing-key-pkcs8");

        OpenBaoCapturedRequest[] tokenRequests = handler.Requests
            .Where(request => request.Path == "/v1/auth/token/create-orphan")
            .ToArray();
        tokenRequests.Length.ShouldBe(2);
        foreach (OpenBaoCapturedRequest request in tokenRequests)
        {
            request.Body.ShouldContain("\"no_default_policy\":true");
            request.Body.ShouldContain("\"renewable\":false");
            request.Body.ShouldContain("\"ttl\":\"168h\"");
            request.Body.ShouldContain("\"explicit_max_ttl\":\"168h\"");
            request.Body.ShouldContain("\"type\":\"service\"");
        }

        int revokeIndex = handler.Requests.FindIndex(request => request.Path == "/v1/auth/token/revoke-self");
        int deniedRootIndex = handler.Requests.FindIndex(request =>
            request.Path == "/v1/auth/token/lookup-self" && request.Token == "root-test-token");
        revokeIndex.ShouldBeGreaterThan(0);
        deniedRootIndex.ShouldBeGreaterThan(revokeIndex);
        handler.Requests[^1].Path.ShouldBe("/v1/sys/health");
    }

    [Fact]
    public async Task InitializeAsync_OpenBaoFailureDoesNotExposeResponseBodyOrSeedValues()
    {
        const string responseCanary = "response-secret-canary";
        const string seedCanary = "seed-secret-canary";
        var handler = new OpenBaoRecordingHandler { InitializationFailureBody = responseCanary };
        using var client = new HttpClient(handler);
        var initializer = new OpenBaoInitializer(client);
        OpenBaoSeedInputs seeds = OpenBaoSeedInputs.Create(
            $$"""{"provider-secret":"{{seedCanary}}"}""",
            "{\"access-telemetry-marker-key\":\"marker\",\"access-telemetry-clock-key\":\"clock\"}");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            initializer.InitializeAsync(Endpoint, seeds, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("read initialization status", Case.Insensitive);
        exception.Message.ShouldContain(((int)HttpStatusCode.InternalServerError).ToString());
        exception.Message.ShouldNotContain(responseCanary, Case.Sensitive);
        exception.Message.ShouldNotContain(seedCanary, Case.Sensitive);
    }

    [Fact]
    public void SeedInputs_InvalidRuntimeSecretNameFailsWithoutDisclosingValue()
    {
        const string valueCanary = "do-not-disclose-this-value";

        ArgumentException exception = Should.Throw<ArgumentException>(() => OpenBaoSeedInputs.Create(
            $$"""{"../invalid":"{{valueCanary}}"}""",
            "{\"access-telemetry-marker-key\":\"marker\",\"access-telemetry-clock-key\":\"clock\"}"));

        exception.Message.ShouldContain("secret name", Case.Insensitive);
        exception.Message.ShouldNotContain(valueCanary, Case.Sensitive);
    }

    private static OpenBaoCapturedRequest Single(
        OpenBaoRecordingHandler handler,
        HttpMethod method,
        string path)
        => handler.Requests.Single(request => request.Method == method && request.Path == path);

    private static void AssertSeed(OpenBaoRecordingHandler handler, string path, string field)
    {
        OpenBaoCapturedRequest request = Single(handler, HttpMethod.Post, path);
        using JsonDocument document = JsonDocument.Parse(request.Body);
        JsonElement data = document.RootElement.GetProperty("data");
        data.EnumerateObject().Select(property => property.Name).ShouldBe([field]);
    }

    private static string PolicyText(OpenBaoCapturedRequest request)
    {
        using JsonDocument document = JsonDocument.Parse(request.Body);
        return document.RootElement.GetProperty("policy").GetString()!;
    }
}
