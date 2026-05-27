// <copyright file="BackendHealthResponseWriterTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.HealthChecks;

using System.Text;
using System.Text.Json;

using Hexalith.Memories.ServiceDefaults.Health;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

/// <summary>
/// Story 8.1 Task 3 — JSON schema and semantic guarantees for
/// <see cref="BackendHealthResponseWriter"/>. Pins the V1 field manifest
/// (see AC #7) + the capability-affected mapping + the AOT-guard roundtrip
/// (any future regression where source-generation is enabled on the writer would
/// silently emit <c>{}</c> without this guard).
/// </summary>
public class BackendHealthResponseWriterTests
{
    [Fact]
    public async Task WriteAsync_AllHealthy_EmitsHealthyStatusAndNoCapabilities()
    {
        HealthReport report = BuildReport(
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(12),
            ("redisearch", HealthStatus.Healthy, "ok", 2),
            ("falkordb", HealthStatus.Healthy, "ok", 3));

        JsonElement root = await WriteAndParseAsync(report);

        root.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        root.GetProperty("status").GetString().ShouldBe("Healthy");
        root.GetProperty("totalDurationMs").GetInt32().ShouldBe(12);

        JsonElement entries = root.GetProperty("entries");
        entries.GetProperty("redisearch").GetProperty("status").GetString().ShouldBe("Healthy");
        entries.GetProperty("redisearch").GetProperty("affectedCapabilities").GetArrayLength().ShouldBe(0);
        entries.GetProperty("falkordb").GetProperty("affectedCapabilities").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task WriteAsync_OneBackendDegraded_PopulatesCapabilities()
    {
        HealthReport report = BuildReport(
            HealthStatus.Degraded,
            TimeSpan.FromMilliseconds(42),
            ("redisearch", HealthStatus.Healthy, "ok", 2),
            ("falkordb", HealthStatus.Degraded, "FalkorDB unreachable: RedisConnectionException", 38));

        JsonElement root = await WriteAndParseAsync(report);

        root.GetProperty("status").GetString().ShouldBe("Degraded");

        JsonElement falkor = root.GetProperty("entries").GetProperty("falkordb");
        falkor.GetProperty("status").GetString().ShouldBe("Degraded");
        falkor.GetProperty("description").GetString()!.ShouldContain("FalkorDB unreachable");
        string[] capabilities = falkor.GetProperty("affectedCapabilities")
            .EnumerateArray()
            .Select(e => e.GetString()!)
            .ToArray();
        capabilities.ShouldContain("graph-traversal");
        capabilities.ShouldContain("graph-scoped-search");
    }

    [Fact]
    public async Task WriteAsync_SidecarUnhealthy_PopulatesSidecarCapabilities()
    {
        HealthReport report = BuildReport(
            HealthStatus.Unhealthy,
            TimeSpan.FromMilliseconds(3000),
            ("dapr-sidecar", HealthStatus.Unhealthy, "Dapr sidecar is not responsive.", 3000));

        JsonElement root = await WriteAndParseAsync(report);

        root.GetProperty("status").GetString().ShouldBe("Unhealthy");
        JsonElement sidecar = root.GetProperty("entries").GetProperty("dapr-sidecar");
        sidecar.GetProperty("status").GetString().ShouldBe("Unhealthy");
        sidecar.GetProperty("affectedCapabilities").EnumerateArray()
            .Select(e => e.GetString()!)
            .ShouldContain("workflow-orchestration");
    }

    [Fact]
    public async Task WriteAsync_UnknownCheckName_ReturnsEmptyCapabilitiesWithoutThrowing()
    {
        HealthReport report = BuildReport(
            HealthStatus.Degraded,
            TimeSpan.FromMilliseconds(5),
            ("mystery-check", HealthStatus.Degraded, "unknown", 5));

        JsonElement root = await WriteAndParseAsync(report);

        JsonElement mystery = root.GetProperty("entries").GetProperty("mystery-check");
        mystery.GetProperty("status").GetString().ShouldBe("Degraded");
        mystery.GetProperty("affectedCapabilities").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task WriteAsync_SetsJsonContentType()
    {
        HealthReport report = BuildReport(
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(1),
            ("redisearch", HealthStatus.Healthy, "ok", 1));

        DefaultHttpContext context = new();
        MemoryStream buffer = new();
        context.Response.Body = buffer;

        await BackendHealthResponseWriter.WriteAsync(context, report);

        context.Response.ContentType.ShouldBe("application/json; charset=utf-8");
    }

    [Fact]
    public async Task WriteAsync_EmitsStableV1FieldManifest()
    {
        HealthReport report = BuildReport(
            HealthStatus.Degraded,
            TimeSpan.FromMilliseconds(10),
            ("redisearch", HealthStatus.Healthy, "ok", 2),
            ("redis-vector", HealthStatus.Degraded, "Vector module absent", 3));

        JsonElement root = await WriteAndParseAsync(report);

        // Top-level manifest — breaking changes must bump SchemaVersion and be documented.
        string[] topLevel = [.. root.EnumerateObject().Select(p => p.Name)];
        topLevel.ShouldContain("schemaVersion");
        topLevel.ShouldContain("status");
        topLevel.ShouldContain("totalDurationMs");
        topLevel.ShouldContain("entries");

        // Per-entry manifest — sampled on one entry.
        JsonElement entry = root.GetProperty("entries").GetProperty("redis-vector");
        string[] entryManifest = [.. entry.EnumerateObject().Select(p => p.Name)];
        entryManifest.ShouldContain("status");
        entryManifest.ShouldContain("description");
        entryManifest.ShouldContain("durationMs");
        entryManifest.ShouldContain("affectedCapabilities");

        // Type guards (AOT-regression trap — anonymous types silently flatten if source-gen
        // is ever enabled on this writer without a named-record conversion).
        root.GetProperty("schemaVersion").ValueKind.ShouldBe(JsonValueKind.Number);
        entry.GetProperty("description").ValueKind.ShouldBe(JsonValueKind.String);
        entry.GetProperty("durationMs").ValueKind.ShouldBe(JsonValueKind.Number);
        entry.GetProperty("affectedCapabilities").ValueKind.ShouldBe(JsonValueKind.Array);
    }

    private static async Task<JsonElement> WriteAndParseAsync(HealthReport report)
    {
        DefaultHttpContext context = new();
        MemoryStream buffer = new();
        context.Response.Body = buffer;

        await BackendHealthResponseWriter.WriteAsync(context, report);

        string body = Encoding.UTF8.GetString(buffer.ToArray());
        body.ShouldNotBeNullOrWhiteSpace();

        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    private static HealthReport BuildReport(
        HealthStatus aggregate,
        TimeSpan totalDuration,
        params (string Name, HealthStatus Status, string Description, int DurationMs)[] entries)
    {
        Dictionary<string, HealthReportEntry> map = entries.ToDictionary(
            e => e.Name,
            e => new HealthReportEntry(
                status: e.Status,
                description: e.Description,
                duration: TimeSpan.FromMilliseconds(e.DurationMs),
                exception: null,
                data: null,
                tags: null));

        return new HealthReport(map, aggregate, totalDuration);
    }
}
