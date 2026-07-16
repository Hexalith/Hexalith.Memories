// <copyright file="IngestionIntegrationTestDriver.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using NFalkorDB;

using StackExchange.Redis;

using MemoriesCase = Hexalith.Memories.Contracts.V1.Case;

/// <summary>Shared bounded API and backing-store driver for Aspire ingestion integration tests.</summary>
public sealed class IngestionIntegrationTestDriver
{
    /// <summary>Default convergence budget for a real DAPR workflow.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly AspireIngestionPipelineFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="IngestionIntegrationTestDriver"/> class.</summary>
    /// <param name="fixture">Running Aspire fixture.</param>
    public IngestionIntegrationTestDriver(AspireIngestionPipelineFixture fixture)
        => _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

    /// <summary>Provisions an active tenant and creates a unique case.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="vectorDimensions">Optional vector dimension override.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The created case identifier.</returns>
    public async Task<string> CreateTenantAndCaseAsync(
        string tenantId,
        int? vectorDimensions = null,
        CancellationToken cancellationToken = default)
    {
        _ = await _fixture.ProvisionActiveTenantAsync(
            tenantId,
            vectorDimensions: vectorDimensions,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return await CreateCaseAsync(tenantId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a case for an active tenant.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="caseId">Optional case identifier.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The created case identifier.</returns>
    public async Task<string> CreateCaseAsync(
        string tenantId,
        string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        string id = caseId ?? $"case-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/cases",
            new CreateCaseInput(tenantId, id, "Story 26.3 integration closure case."),
            MemoriesJsonContext.Options,
            cancellationToken).ConfigureAwait(false);
        await EnsureStatusAsync(response, HttpStatusCode.Created, cancellationToken).ConfigureAwait(false);

        MemoriesCase? created = await response.Content.ReadFromJsonAsync<MemoriesCase>(
            MemoriesJsonContext.Options,
            cancellationToken).ConfigureAwait(false);
        return created?.Id is { Length: > 0 } createdId
            ? createdId
            : throw new InvalidOperationException("Case creation returned no case identifier.");
    }

    /// <summary>Posts a URL ingestion request.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="caseId">Case identifier.</param>
    /// <param name="sourceUri">Source URL.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The accepted ingestion response.</returns>
    public async Task<UrlIngestionResponse> PostUrlIngestionAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/v1/ingest/url",
            new UrlIngestionRequest
            {
                TenantId = tenantId,
                CaseId = caseId,
                Url = sourceUri,
                IngestedBy = "integration@test.local",
            },
            MemoriesJsonContext.Options,
            cancellationToken).ConfigureAwait(false);
        await EnsureStatusAsync(response, HttpStatusCode.Accepted, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<UrlIngestionResponse>(
            MemoriesJsonContext.Options,
            cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("URL ingestion returned no accepted payload.");
    }

    /// <summary>Posts inline content ingestion.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="caseId">Case identifier.</param>
    /// <param name="sourceUri">Stable source URI.</param>
    /// <param name="content">UTF-8 source content.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The accepted workflow instance identifier.</returns>
    public async Task<string> PostInlineIngestionAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        string content,
        CancellationToken cancellationToken = default)
    {
        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = sourceUri,
            ContentBytes = Encoding.UTF8.GetBytes(content),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/v1/ingest",
            input,
            MemoriesJsonContext.Options,
            cancellationToken).ConfigureAwait(false);
        await EnsureStatusAsync(response, HttpStatusCode.Accepted, cancellationToken).ConfigureAwait(false);
        using JsonDocument accepted = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        return accepted.RootElement.TryGetProperty("instanceId", out JsonElement instanceId) &&
            instanceId.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(instanceId.GetString())
                ? instanceId.GetString()!
                : throw new InvalidOperationException("Inline ingestion returned no workflow instance identifier.");
    }

    /// <summary>Waits for a workflow to reach the requested DAPR runtime status.</summary>
    /// <param name="tenantId">Tenant identifier used to authenticate the status request.</param>
    /// <param name="instanceId">Workflow instance identifier.</param>
    /// <param name="expectedRuntimeStatus">Expected named runtime status.</param>
    /// <param name="timeout">Optional wait budget.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The terminal workflow JSON payload.</returns>
    public async Task<string> WaitForWorkflowRuntimeStatusAsync(
        string tenantId,
        string instanceId,
        string expectedRuntimeStatus,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource waitCts = CreateWaitCancellation(timeout, cancellationToken);
        string lastPayload = string.Empty;
        try
        {
            while (true)
            {
                waitCts.Token.ThrowIfCancellationRequested();
                using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/ingest/{instanceId}");
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    AspireIngestionPipelineFixture.MintServerBearer(tenantId));
                using HttpResponseMessage response = await _fixture.MemoriesClient.SendAsync(
                    request,
                    waitCts.Token).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    lastPayload = await response.Content.ReadAsStringAsync(waitCts.Token).ConfigureAwait(false);
                    if (ReachedRuntimeStatus(lastPayload, expectedRuntimeStatus))
                    {
                        return lastPayload;
                    }

                    if (TryReadRuntimeStatus(lastPayload, out string actualRuntimeStatus) &&
                        IsTerminalRuntimeStatus(actualRuntimeStatus))
                    {
                        throw new InvalidOperationException(
                            $"Workflow '{instanceId}' reached unexpected terminal runtimeStatus='{actualRuntimeStatus}' " +
                            $"while waiting for '{expectedRuntimeStatus}'. Payload: {lastPayload}");
                    }
                }

                await Task.Delay(PollInterval, waitCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Workflow '{instanceId}' did not reach '{expectedRuntimeStatus}' within " +
                $"{timeout ?? DefaultTimeout}. Last payload: {lastPayload}",
                ex);
        }
    }

    /// <summary>Waits for a failed-units API predicate.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="caseId">Case identifier.</param>
    /// <param name="predicate">Required page condition.</param>
    /// <param name="timeout">Optional wait budget.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The matching failed-units page.</returns>
    public async Task<FailedUnitsPage> WaitForFailedUnitsPageAsync(
        string tenantId,
        string caseId,
        Func<FailedUnitsPage, bool> predicate,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        => await WaitForJsonAsync(
            $"/api/v1/tenants/{tenantId}/cases/{caseId}/failed-units",
            predicate,
            timeout,
            cancellationToken).ConfigureAwait(false);

    /// <summary>Waits for a memory-unit API predicate.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="caseId">Case identifier.</param>
    /// <param name="memoryUnitId">Memory-unit identifier.</param>
    /// <param name="predicate">Required memory-unit condition.</param>
    /// <param name="timeout">Optional wait budget.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The matching memory unit.</returns>
    public async Task<MemoryUnit> WaitForMemoryUnitAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        Func<MemoryUnit, bool> predicate,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        => await WaitForJsonAsync(
            $"/api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}",
            predicate,
            timeout,
            cancellationToken).ConfigureAwait(false);

    /// <summary>Waits for the most recently registered tenant to appear in the paginated enriched tenant list.</summary>
    /// <param name="tenantId">Expected tail tenant identifier.</param>
    /// <param name="predicate">Required enriched-summary condition.</param>
    /// <param name="timeout">Optional wait budget.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The matching enriched tenant summary.</returns>
    public async Task<TenantSummary> WaitForNewestTenantSummaryAsync(
        string tenantId,
        Func<TenantSummary, bool> predicate,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(predicate);

        using CancellationTokenSource waitCts = CreateWaitCancellation(timeout, cancellationToken);
        TenantSummary? last = null;
        int lastTotal = 0;
        while (!waitCts.IsCancellationRequested)
        {
            using HttpResponseMessage firstPage = await _fixture.MemoriesClient.GetAsync(
                "/api/v1/tenants?offset=0&limit=1",
                waitCts.Token).ConfigureAwait(false);
            if (firstPage.StatusCode == HttpStatusCode.OK
                && firstPage.Headers.TryGetValues("X-Hexalith-Total-Count", out IEnumerable<string>? totalValues)
                && int.TryParse(
                    totalValues.SingleOrDefault(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int total)
                && total > 0)
            {
                lastTotal = total;
                using HttpResponseMessage tailPage = await _fixture.MemoriesClient.GetAsync(
                    $"/api/v1/tenants?offset={total - 1}&limit=1",
                    waitCts.Token).ConfigureAwait(false);
                if (tailPage.StatusCode == HttpStatusCode.OK)
                {
                    TenantSummary[]? summaries = await tailPage.Content.ReadFromJsonAsync<TenantSummary[]>(
                        MemoriesJsonContext.Options,
                        waitCts.Token).ConfigureAwait(false);
                    last = summaries?.SingleOrDefault();
                    if (last is not null && string.Equals(last.Id, tenantId, StringComparison.Ordinal) && predicate(last))
                    {
                        return last;
                    }
                }
            }

            await Task.Delay(PollInterval, waitCts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Newest tenant summary did not converge for '{tenantId}'. Last total: {lastTotal}; last tenant: {last?.Id ?? "n/a"}.");
    }

    /// <summary>Lists exact Redis keys matching a scoped pattern.</summary>
    /// <param name="pattern">Redis key pattern.</param>
    /// <returns>The matching keys.</returns>
    public Task<string[]> ListRedisKeysAsync(string pattern)
    {
        IServer server = _fixture.RedisConnection.GetServer(_fixture.RedisConnection.GetEndPoints().Single());
        return Task.FromResult(server.Keys(pattern: pattern).Select(key => key.ToString()).Order().ToArray());
    }

    /// <summary>Counts matching tenant graph nodes.</summary>
    /// <param name="tenantId">Tenant graph name.</param>
    /// <param name="caseId">Case identifier.</param>
    /// <param name="sourceUri">Source URI.</param>
    /// <returns>The graph node count.</returns>
    public async Task<long> CountGraphNodesAsync(string tenantId, string caseId, string sourceUri)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(
            "MATCH (m:MemoryUnit {caseId: $caseId, sourceUri: $sourceUri}) RETURN count(m) as cnt",
            new Dictionary<string, object>
            {
                ["caseId"] = caseId,
                ["sourceUri"] = sourceUri,
            }).ConfigureAwait(false);
        IEnumerator<Record> rows = result.GetEnumerator();
        return rows.MoveNext()
            ? rows.Current.GetValue<long>("cnt")
            : throw new InvalidOperationException("FalkorDB count query returned no row.");
    }

    /// <summary>Waits until one source has converged in Redis syntactic/vector indexes and FalkorDB.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="caseId">Case identifier.</param>
    /// <param name="sourceUri">Source URI.</param>
    /// <param name="timeout">Optional wait budget.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>The single syntactic and semantic keys.</returns>
    public async Task<(string SyntacticKey, string SemanticKey)> WaitForSingleBackendWriteAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource waitCts = CreateWaitCancellation(timeout, cancellationToken);
        while (!waitCts.IsCancellationRequested)
        {
            string[] syntactic = await ListRedisKeysAsync($"{tenantId}:mu:*").ConfigureAwait(false);
            string[] semantic = await ListRedisKeysAsync($"{tenantId}:vec:*").ConfigureAwait(false);
            long graphCount = await CountGraphNodesAsync(tenantId, caseId, sourceUri).ConfigureAwait(false);
            if (syntactic.Length == 1 && semantic.Length == 1 && graphCount == 1)
            {
                return (syntactic[0], semantic[0]);
            }

            await Task.Delay(PollInterval, waitCts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException($"Backing stores did not converge for '{tenantId}/{caseId}' and '{sourceUri}'.");
    }

    /// <summary>Extracts the memory-unit identifier from a workflow serialized output.</summary>
    /// <param name="workflowPayload">Workflow status JSON.</param>
    /// <returns>The memory-unit identifier, or <see langword="null"/> before output materializes.</returns>
    public static string? TryExtractMemoryUnitId(string workflowPayload)
    {
        try
        {
            using JsonDocument outer = JsonDocument.Parse(workflowPayload);
            if (!outer.RootElement.TryGetProperty("serializedOutput", out JsonElement serialized) ||
                serialized.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(serialized.GetString()))
            {
                return null;
            }

            using JsonDocument inner = JsonDocument.Parse(serialized.GetString()!);
            return inner.RootElement.TryGetProperty("memoryUnitId", out JsonElement id) && id.ValueKind == JsonValueKind.String
                ? id.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<T> WaitForJsonAsync<T>(
        string path,
        Func<T, bool> predicate,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource waitCts = CreateWaitCancellation(timeout, cancellationToken);
        while (!waitCts.IsCancellationRequested)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(path, waitCts.Token).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                T? value = await response.Content.ReadFromJsonAsync<T>(
                    MemoriesJsonContext.Options,
                    waitCts.Token).ConfigureAwait(false);
                if (value is not null && predicate(value))
                {
                    return value;
                }
            }

            await Task.Delay(PollInterval, waitCts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException($"Endpoint '{path}' did not satisfy its predicate within the wait budget.");
    }

    private static bool ReachedRuntimeStatus(string payload, string expectedRuntimeStatus)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;
        if (TryReadRuntimeStatus(root, out string actualRuntimeStatus) &&
            string.Equals(actualRuntimeStatus, expectedRuntimeStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(expectedRuntimeStatus, "Completed", StringComparison.OrdinalIgnoreCase) &&
            root.TryGetProperty("isWorkflowCompleted", out JsonElement completed) &&
            completed.ValueKind == JsonValueKind.True;
    }

    private static bool TryReadRuntimeStatus(string payload, out string runtimeStatus)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        return TryReadRuntimeStatus(document.RootElement, out runtimeStatus);
    }

    private static bool TryReadRuntimeStatus(JsonElement root, out string runtimeStatus)
    {
        if (root.TryGetProperty("runtimeStatus", out JsonElement value))
        {
            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                runtimeStatus = value.GetString()!;
                return true;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int ordinal))
            {
                runtimeStatus = ordinal switch
                {
                    3 => "Completed",
                    5 => "Failed",
                    6 => "Canceled",
                    7 => "Terminated",
                    _ => string.Empty,
                };
                return runtimeStatus.Length > 0;
            }
        }

        runtimeStatus = string.Empty;
        return false;
    }

    private static bool IsTerminalRuntimeStatus(string runtimeStatus)
        => string.Equals(runtimeStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(runtimeStatus, "Failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(runtimeStatus, "Canceled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(runtimeStatus, "Terminated", StringComparison.OrdinalIgnoreCase);

    private static CancellationTokenSource CreateWaitCancellation(
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout ?? DefaultTimeout);
        return source;
    }

    private static async Task EnsureStatusAsync(
        HttpResponseMessage response,
        HttpStatusCode expected,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == expected)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException(
            $"Expected HTTP {(int)expected}, received {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
    }
}
