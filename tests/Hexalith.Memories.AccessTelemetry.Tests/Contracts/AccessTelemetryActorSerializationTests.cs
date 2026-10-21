// <copyright file="AccessTelemetryActorSerializationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Contracts;

using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Communication;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using Shouldly;

/// <summary>Exercises HTTP JSON and the pinned SDK's actual actor remoting serializer.</summary>
public sealed class AccessTelemetryActorSerializationTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("tenant-lifecycle")]
    [InlineData("search")]
    [InlineData("traverse")]
    public async Task HttpBatch_RoundTripsTheFullGraphThroughTheSdkDataContractProvider(string operation)
    {
        AccessTelemetryWriteBatchRequest original = CreateRequest(operation);
        byte[] httpJson = JsonSerializer.SerializeToUtf8Bytes(original, WebJson);
        AccessTelemetryWriteBatchRequest decoded = JsonSerializer.Deserialize<AccessTelemetryWriteBatchRequest>(httpJson, WebJson).ShouldNotBeNull();
        decoded.Records.ShouldBeOfType<List<AccessTelemetryRecord>>();

        AccessTelemetryWriteBatchRequest restored = await RoundTripActorRequestAsync(decoded);

        restored.ConfigurationEpoch.ShouldBe(original.ConfigurationEpoch);
        restored.ComponentProfileHash.ShouldBe(original.ComponentProfileHash);
        restored.RequestingProcessEpoch.ShouldBe(original.RequestingProcessEpoch);
        restored.RequestingServiceInstanceId.ShouldBe(original.RequestingServiceInstanceId);
        restored.ClockAttestation.ShouldBe(original.ClockAttestation);
        restored.Records.Count.ShouldBe(1);
        AccessTelemetryCanonicalizer.CanonicalizeRecord(restored.Records[0])
            .ShouldBe(AccessTelemetryCanonicalizer.CanonicalizeRecord(original.Records[0]));
        restored.Records[0].EnvelopeHash.ShouldBe(original.Records[0].EnvelopeHash);
        restored.Records[0].QueryParams.Values.ShouldNotContain(value => value is JsonElement);
    }

    [Theory]
    [InlineData("{\"action\":{\"nested\":true},\"workflowState\":\"completed\"}")]
    [InlineData("{\"action\":[\"provision\"],\"workflowState\":\"completed\"}")]
    [InlineData("{\"action\":1.5,\"workflowState\":\"completed\"}")]
    [InlineData("{\"action\":2147483648,\"workflowState\":\"completed\"}")]
    [InlineData("null")]
    [InlineData("{\"action\":\"provision\",\"action\":\"provision\",\"workflowState\":\"completed\"}")]
    public void HttpBatch_RefusesNonScalarOrOutOfRangeQueryValues(string query)
    {
        string json = JsonSerializer.Serialize(CreateRequest("tenant-lifecycle"), WebJson);
        using JsonDocument document = JsonDocument.Parse(json);
        string originalQuery = document.RootElement.GetProperty("records")[0].GetProperty("queryParams").GetRawText();
        string changed = json.Replace(originalQuery, query, StringComparison.Ordinal);

        Should.Throw<AccessTelemetryContractException>(() => JsonSerializer.Deserialize<AccessTelemetryWriteBatchRequest>(changed, WebJson));
    }

    [Fact]
    public async Task RoundTrip_PreservesTamperedHashForExistingCanonicalValidationToRefuse()
    {
        AccessTelemetryWriteBatchRequest request = CreateRequest("tenant-lifecycle");
        string tamperedHash = new('f', 64);
        request = request with { Records = new List<AccessTelemetryRecord> { request.Records[0] with { EnvelopeHash = tamperedHash } } };
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(request, WebJson);
        AccessTelemetryWriteBatchRequest decoded = JsonSerializer.Deserialize<AccessTelemetryWriteBatchRequest>(json, WebJson).ShouldNotBeNull();

        AccessTelemetryWriteBatchRequest restored = await RoundTripActorRequestAsync(decoded);

        restored.Records[0].EnvelopeHash.ShouldBe(tamperedHash);
        Should.Throw<AccessTelemetryContractException>(() => AccessTelemetryCanonicalizer.CanonicalizeRecord(restored.Records[0]))
            .Message.ShouldBe("envelope_hash_invalid");
    }

    private static async Task<AccessTelemetryWriteBatchRequest> RoundTripActorRequestAsync(AccessTelemetryWriteBatchRequest request)
    {
        // This SDK provider is internal. Invoke the pinned implementation, retaining its real binary XML wire format.
        Type providerType = typeof(ActorId).Assembly.GetType("Dapr.Actors.Communication.ActorMessageBodyDataContractSerializationProvider").ShouldNotBeNull();
        object provider = Activator.CreateInstance(providerType, nonPublic: true).ShouldNotBeNull();
        var factory = (IActorMessageBodyFactory)providerType.GetMethod("CreateMessageBodyFactory")!.Invoke(provider, null).ShouldNotBeNull();
        object serializer = providerType.GetMethod("CreateRequestMessageBodySerializer")!.Invoke(provider,
            [typeof(IAccessTelemetryLifecycleActor), new[] { typeof(AccessTelemetryWriteBatchRequest) }, new[] { typeof(BatchMessage) }]).ShouldNotBeNull();
        Type serializerInterface = typeof(ActorId).Assembly.GetType("Dapr.Actors.Communication.IActorRequestMessageBodySerializer").ShouldNotBeNull();
        IActorRequestMessageBody body = factory.CreateRequestMessageBody(
            nameof(IAccessTelemetryLifecycleActor), nameof(IAccessTelemetryLifecycleActor.WriteBatchAsync), 1,
            new BatchMessage { Request = request });
        byte[] bytes;
        try
        {
            bytes = (byte[])serializerInterface.GetMethod("Serialize")!.Invoke(serializer, [body])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        bytes.Length.ShouldBeGreaterThan(0);
        await using var stream = new MemoryStream(bytes);
        var pending = (ValueTask<IActorRequestMessageBody>)serializerInterface.GetMethod("DeserializeAsync")!.Invoke(serializer, [stream])!;
        IActorRequestMessageBody restored = await pending;
        return ((BatchMessage)restored.GetType().GetProperty("Value")!.GetValue(restored)!).Request;
    }

    private static AccessTelemetryWriteBatchRequest CreateRequest(string operation)
    {
        IReadOnlyDictionary<string, object?> query = operation switch
        {
            "search" => new Dictionary<string, object?>
            {
                ["axis"] = "hybrid", ["caseScope"] = "all-authorized", ["explain"] = false,
                ["queryLengthBucket"] = "33-128", ["subjectPresent"] = true, ["weightProfile"] = "configured",
            },
            "traverse" => new Dictionary<string, object?>
            {
                ["caseScope"] = "all-authorized", ["depthBucket"] = "2", ["direction"] = "out", ["edgeTypeCount"] = 3, ["includeGaps"] = false,
            },
            _ => new Dictionary<string, object?> { ["action"] = "provision", ["workflowState"] = "completed" },
        };
        AccessTelemetryRecord record = new()
        {
            SchemaVersion = 1, RecordId = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9", EventId = operation == "search" ? 7501 : operation == "traverse" ? 7503 : 7506,
            EmittedAtUtc = "2026-07-18T08:00:00.000Z", AcceptedAtUtc = "2026-07-18T08:00:00.100Z", ExpiresAtUtc = "2026-07-19T08:00:00.000Z",
            TenantMarker = new string('a', 64), UserMarker = new string('b', 64), MarkerKeyId = "mk-2026a", OperationType = operation,
            QueryParams = query, ResultCount = operation is "search" or "traverse" ? 10 : null, DurationMs = 42, Outcome = "ok", EnvelopeHash = string.Empty,
        };
        record = record with { EnvelopeHash = AccessTelemetryCanonicalizer.CalculateEnvelopeHash(record) };
        return new AccessTelemetryWriteBatchRequest
        {
            ConfigurationEpoch = "01HM5Q9WXGK6T8Q4Z5Y6V7W8X9", ComponentProfileHash = new string('c', 64),
            RequestingProcessEpoch = "writer-process", RequestingServiceInstanceId = "writer-instance", Records = new List<AccessTelemetryRecord> { record },
            ClockAttestation = new SignedClockAttestation
            {
                DeploymentId = "deployment", AppId = "memories", ServiceInstanceId = "clock-instance", ProcessEpoch = "clock-process",
                ComponentProfileHash = new string('c', 64), RequestingProcessEpoch = "writer-process", RequestingServiceInstanceId = "writer-instance",
                Nonce = "nonce", NotBeforeUnixMilliseconds = 1000, NotAfterUnixMilliseconds = 1100, IssuedAtUnixMilliseconds = 1000,
                ExpiresAtUnixMilliseconds = 2000, SignerKeyEpoch = "clock-key", Signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("serialization-only-signature")),
            },
        };
    }

    [DataContract]
    public sealed class BatchMessage
    {
        [DataMember]
        public required AccessTelemetryWriteBatchRequest Request { get; init; }
    }
}
