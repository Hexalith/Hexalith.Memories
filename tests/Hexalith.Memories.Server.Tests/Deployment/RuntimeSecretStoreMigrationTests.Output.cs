// <copyright file="RuntimeSecretStoreMigrationTests.Output.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System.Diagnostics;
using System.Net;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Diagnostics;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tests.Ingestion;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

public sealed partial class RuntimeSecretStoreMigrationTests
{
    [Fact]
    public async Task ResolvedCredential_StaysOutOfLogsTelemetryAndResponseSnapshotAsync()
    {
        string canary = "synthetic-" + Guid.NewGuid().ToString("N");
        var config = EmbeddingProviderDefaults.Google();
        using var dapr = Substitute.For<DaprClient>();
        dapr.GetSecretAsync("secretstore", config.ApiSecretKeyName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { [config.ApiSecretKeyName] = canary });
        var provider = new GoogleEmbeddingProvider(new EmbeddingSecretStore(dapr), "https://provider.invalid");
        bool received = false;
        using var http = new HttpClient(new EmbeddingClientConfigTests.TestDelegatingHandler((request, _) =>
        {
            string header = request.Headers.GetValues("x-goog-api-key").Single();
            received = header == canary;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(header) });
        }));
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("EmbeddingClient").Returns(http);
        var transport = new EmbeddingProviderTransport(factory);
        EmbeddingApiException failure;
        try
        {
            await transport.ExecuteAsync(provider, ["input"], "tenant-a", config, false, CancellationToken.None);
            throw new ShouldAssertException("Expected a rejected embedding request.");
        }
        catch (EmbeddingApiException ex)
        {
            failure = ex;
        }

        received.ShouldBeTrue("The simulated provider must receive the Dapr-resolved credential.");
        using CapturingAuditLoggerProvider logs = new();
        using ILoggerFactory loggers = LoggerFactory.Create(builder => builder.AddProvider(logs));
        var handler = new MemoriesServerExceptionHandler(loggers.CreateLogger<MemoriesServerExceptionHandler>(), loggers.CreateLogger<AccessTelemetryCategory>());
        var context = new DefaultHttpContext();
        using var response = new MemoryStream();
        context.Response.Body = response;
        using var activity = new Activity("migration-error").Start();
        (await handler.TryHandleAsync(context, failure, CancellationToken.None)).ShouldBeTrue();
        logs.Captures.Count.ShouldBeGreaterThan(0);
        logs.AccessTelemetryCaptures.Count.ShouldBe(1);
        foreach (var log in logs.Captures)
        {
            AssertNoCanary(log.Message + log.Exception + JsonSerializer.Serialize(log.Fields), canary, "log");
        }

        AssertNoCanary(JsonSerializer.Serialize(new { activity.Tags, activity.TagObjects, activity.Events, activity.StatusDescription, Audit = logs.AccessTelemetryCaptures[0].AuditEvent }), canary, "telemetry");
        response.Position = 0;
        using var reader = new StreamReader(response);
        string snapshot = await reader.ReadToEndAsync();
        AssertNoCanary(snapshot, canary, "response snapshot");
        snapshot.Contains("UNHANDLED_EXCEPTION", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public async Task OllamaCredentialsAndDaprStoreFailure_StayOutOfLogsTelemetryAndResponseSnapshotAsync()
    {
        string clientSecret = "synthetic-client-" + Guid.NewGuid().ToString("N");
        string bearerToken = "synthetic-bearer-" + Guid.NewGuid().ToString("N");
        var config = new TenantEmbeddingConfig
        {
            Provider = "ollama",
            Model = "nomic-embed-text",
            Dimensions = 768,
            RateLimitPerMinute = 60,
            ApiSecretKeyName = "memories-embedding-client-secret",
            BaseUrl = "https://ollama.invalid",
            AuthMode = "oidc-client-credentials",
            OidcTokenEndpoint = "https://idp.invalid/token",
            OidcClientId = "memories-client",
            OidcScope = "embeddings",
        };
        using var dapr = Substitute.For<DaprClient>();
        dapr.GetSecretAsync("secretstore", config.ApiSecretKeyName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { [config.ApiSecretKeyName] = clientSecret });
        var oidc = Substitute.For<IOidcTokenProvider>();
        oidc.GetAccessTokenAsync(config.OidcTokenEndpoint!, config.OidcClientId!, clientSecret, config.OidcScope, Arg.Any<CancellationToken>())
            .Returns(bearerToken);
        var provider = new OllamaEmbeddingProvider(new EmbeddingSecretStore(dapr), oidc);
        bool receivedBearer = false;
        using var http = new HttpClient(new EmbeddingClientConfigTests.TestDelegatingHandler((request, _) =>
        {
            receivedBearer = request.Headers.Authorization?.Parameter == bearerToken;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent($"echo {clientSecret} and {bearerToken}"),
            });
        }));
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("EmbeddingClient").Returns(http);
        var transport = new EmbeddingProviderTransport(factory);

        EmbeddingApiException ollamaFailure = await Should.ThrowAsync<EmbeddingApiException>(
            () => transport.ExecuteAsync(provider, ["input"], "tenant-a", config, false, CancellationToken.None));
        receivedBearer.ShouldBeTrue("The simulated Ollama provider must receive the OIDC bearer token.");

        using var failingDapr = Substitute.For<DaprClient>();
        string daprErrorCanary = "synthetic-dapr-err-" + Guid.NewGuid().ToString("N");
        failingDapr.GetSecretAsync("secretstore", config.ApiSecretKeyName, cancellationToken: Arg.Any<CancellationToken>())
            .Returns<Dictionary<string, string>>(_ => throw new InvalidOperationException("sidecar failure " + daprErrorCanary));
        EmbeddingApiException storeFailure = await Should.ThrowAsync<EmbeddingApiException>(
            () => new EmbeddingSecretStore(failingDapr).GetSecretAsync("tenant-a", config.ApiSecretKeyName, CancellationToken.None));

        var capturedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == "Hexalith.Memories.MigrationOutputTest",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = capturedActivities.Add,
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("Hexalith.Memories.MigrationOutputTest");

        foreach ((EmbeddingApiException failure, string[] canaries, bool checkLogExceptions) in new[]
        {
            (ollamaFailure, new[] { clientSecret, bearerToken }, true),
            (storeFailure, new[] { daprErrorCanary }, false),
        })
        {
            using CapturingAuditLoggerProvider logs = new();
            using ILoggerFactory loggers = LoggerFactory.Create(builder => builder.AddProvider(logs));
            var handler = new MemoriesServerExceptionHandler(
                loggers.CreateLogger<MemoriesServerExceptionHandler>(),
                loggers.CreateLogger<AccessTelemetryCategory>());
            var context = new DefaultHttpContext();
            using var response = new MemoryStream();
            context.Response.Body = response;
            using (Activity? activity = source.StartActivity("migration-error"))
            {
                activity.ShouldNotBeNull();
                (await handler.TryHandleAsync(context, failure, CancellationToken.None)).ShouldBeTrue();
            }

            logs.Captures.Count.ShouldBeGreaterThan(0);
            logs.AccessTelemetryCaptures.Count.ShouldBe(1);
            string auditSerialized = JsonSerializer.Serialize(logs.AccessTelemetryCaptures[0].AuditEvent);
            string activitySerialized = JsonSerializer.Serialize(capturedActivities[^1], new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles });
            response.Position = 0;
            using var reader = new StreamReader(response);
            string snapshot = await reader.ReadToEndAsync();
            snapshot.Contains("UNHANDLED_EXCEPTION", StringComparison.Ordinal).ShouldBeTrue();

            foreach (string canary in canaries)
            {
                if (checkLogExceptions)
                {
                    foreach (var log in logs.Captures)
                    {
                        AssertNoCanary(log.Message + log.Exception + JsonSerializer.Serialize(log.Fields), canary, "log");
                    }
                }

                AssertNoCanary(auditSerialized + activitySerialized, canary, "telemetry");
                AssertNoCanary(snapshot, canary, "response snapshot");
            }
        }
    }

    private static void AssertNoCanary(string output, string canary, string channel)
    {
        if (output.Contains(canary, StringComparison.Ordinal))
        {
            throw new ShouldAssertException($"Resolved credential reached {channel}.");
        }
    }
}
