// <copyright file="TelemetryHealthExclusionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using Hexalith.Memories.ServiceDefaults;

using Microsoft.AspNetCore.Http;

using Shouldly;

/// <summary>
/// Story 7.5 Task 9.4 / AC #5 — regression guard that <c>/health</c>, <c>/alive</c>, <c>/ready</c> probes
/// are excluded from AspNetCore trace emission by <see cref="Extensions.ShouldTraceHttpRequest"/>. A silent
/// removal of the filter would flood the trace collector with 1-second health-probe spans from the
/// quickstart wizard (Story 7.4) — this test fails loudly on any such regression.
/// </summary>
public sealed class TelemetryHealthExclusionTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/ready")]
    [InlineData("/health/details")]
    [InlineData("/ALIVE")]
    [InlineData("/Ready")]
    public void ShouldTraceHttpRequest_HealthProbeVariants_ReturnsFalse(string path)
    {
        HttpContext context = BuildContext(path);

        Extensions.ShouldTraceHttpRequest(context).ShouldBeFalse();
    }

    [Theory]
    [InlineData("/api/search")]
    [InlineData("/api/ingest")]
    [InlineData("/api/tenants/demo/traverse")]
    [InlineData("/api/tenants/demo/cases/c1/memory-units/mu-1")]
    [InlineData("/api/tenants/demo/telemetry/summary")]
    [InlineData("/")]
    [InlineData("/metrics")]
    public void ShouldTraceHttpRequest_OperationPaths_ReturnsTrue(string path)
    {
        HttpContext context = BuildContext(path);

        Extensions.ShouldTraceHttpRequest(context).ShouldBeTrue();
    }

    [Fact]
    public void ShouldTraceHttpRequest_NullContext_Throws() =>
        Should.Throw<ArgumentNullException>(() => Extensions.ShouldTraceHttpRequest(null!));

    private static HttpContext BuildContext(string path)
    {
        DefaultHttpContext context = new();
        context.Request.Path = new PathString(path);
        return context;
    }
}
