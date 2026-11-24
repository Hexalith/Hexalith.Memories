// <copyright file="ServerActivityStreamReaderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;

using Shouldly;

[Trait("Category", "Integration")]
public class ServerActivityStreamReaderTests
{
    [Fact]
    public void ScanCapturedLogs_ParsesBreadcrumbAndSkipsOrdinaryLogs()
    {
        const string Json = """{"sourceName":"source","operationName":"operation","traceId":"trace","spanId":"span","kind":"Client"}""";

        var captured = ServerActivityStreamReader.ScanCapturedLogs(
            [
                Log($"ordinary log mentioning {InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix}"),
                Log(InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix, "Hexalith.Memories.AppHost.Resources.memories-mcp"),
                Log($"   16: 2026-11-24T12:00:00.0000000Z {InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix}{Json}"),
            ], 42)
            .ShouldHaveSingleItem();

        captured.TraceId.ShouldBe("trace");
        captured.ParentSpanId.ShouldBeNull();
        captured.RawJsonPayload.ShouldBe(Json);
    }

    [Theory]
    [InlineData("")]
    [InlineData("""{"sourceName":"synthetic-private-value""")]
    [InlineData("""["synthetic-private-value"]""")]
    [InlineData("""{"sourceName":"synthetic-private-value"}""")]
    [InlineData("""{"sourceName":"synthetic-private-value","operationName":"operation","traceId":42,"spanId":"span","kind":"Client"}""")]
    [InlineData("""{"sourceName":"synthetic-private-value","operationName":"operation","traceId":"trace","spanId":"span","kind":"Client","parentSpanId":42}""")]
    public void ScanCapturedLogs_ReportsMalformedBreadcrumbWithoutPayload(string json)
    {
        var exception = Should.Throw<InvalidDataException>(() => ServerActivityStreamReader.ScanCapturedLogs(
            [Log("ordinary log"), Log($"{InMemoryTelemetryEnvironment.ActivityBreadcrumbPrefix}{json}")], 42));

        exception.Message.ShouldContain("captured log index 43");
        exception.Message.ShouldContain("Malformed server activity breadcrumb");
        exception.Message.ShouldNotContain("synthetic-private-value");
        exception.InnerException.ShouldBeNull();
    }

    private static AspireIngestionPipelineFixture.CapturedLogEntry Log(string message, string category = "Hexalith.Memories.AppHost.Resources.memories")
        => new(LogLevel.Error, category, message);
}
