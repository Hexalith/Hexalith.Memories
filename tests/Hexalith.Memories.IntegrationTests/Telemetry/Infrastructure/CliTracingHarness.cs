// <copyright file="CliTracingHarness.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Hexalith.Memories.Telemetry;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Story 8.4 — in-test-process CLI-side tracing harness. Builds a <see cref="TracerProvider"/> that
/// listens on the <see cref="MemoriesActivitySource"/> (CLI root span) and the
/// <c>System.Net.Http</c> source (HttpClient instrumentation) and drains every emitted activity
/// into the supplied <see cref="InMemorySpanCollector"/>.
/// <para>
/// This is the CLI-side leg of the hybrid capture model documented in the story Change Log Rev 0.6:
/// the Memories Server runs as a separate process under Aspire orchestration, so the in-test-process
/// tracer cannot capture Server-emitted activities. Server-side evidence is recovered via the audit
/// log JSON line that <c>AccessTelemetryLog.CreateEvent</c> stamps with
/// <see cref="Activity.Current"/> trace + span ids on the Server side; the test reads that line via
/// the fixture's <c>_logProvider</c> and asserts trace-id equality with the CLI's emitted spans —
/// proof that the W3C <c>traceparent</c> header propagated across the HTTP boundary.
/// </para>
/// <para>
/// The harness uses <see cref="AlwaysOnSampler"/> (Sampler invariant from Dev Notes) so head-based
/// sampling cannot silently drop the root span and make AC #1 flaky.
/// </para>
/// </summary>
internal sealed class CliTracingHarness : IAsyncDisposable
{
    private readonly TracerProvider _tracerProvider;

    private CliTracingHarness(TracerProvider tracerProvider, InMemorySpanCollector collector)
    {
        _tracerProvider = tracerProvider;
        Collector = collector;
    }

    /// <summary>Gets the in-memory span collector the harness drains into.</summary>
    public InMemorySpanCollector Collector { get; }

    /// <summary>Builds the harness with a fresh collector. The returned harness owns the tracer
    /// provider; dispose to flush + tear down.</summary>
    /// <returns>A configured harness with collector ready to capture.</returns>
    public static CliTracingHarness Create()
    {
        InMemorySpanCollector collector = new();
        TracerProvider tracerProvider = Sdk.CreateTracerProviderBuilder()
            .ConfigureResource(r => r.AddService("Hexalith.Memories.Cli", serviceVersion: "8.4"))
            .SetSampler(new AlwaysOnSampler())
            .AddSource(MemoriesActivitySource.SourceName)
            .AddHttpClientInstrumentation()
            .AddProcessor(new SimpleActivityExportProcessor(new CollectionExporter(collector)))
            .Build()!;

        return new CliTracingHarness(tracerProvider, collector);
    }

    /// <summary>Force-flushes both the tracer provider so all in-flight activities reach the
    /// collector before assertions read it (Risk 10 mitigation for the activity side).</summary>
    /// <param name="timeout">Flush timeout.</param>
    /// <returns>A task that completes when flush is done.</returns>
    public Task ForceFlushAsync(TimeSpan timeout)
    {
        // OpenTelemetry's TracerProvider.ForceFlush(int) is synchronous; wrap in Task.Run so callers
        // can await with a timeout enforced by their own CancellationToken.
        return Task.Run(() => _tracerProvider.ForceFlush((int)timeout.TotalMilliseconds));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await ForceFlushAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        _tracerProvider.Dispose();
    }

    /// <summary>Tiny <see cref="BaseExporter{T}"/> that drains each batch into a collector.
    /// Avoids taking a hard dependency on <c>OpenTelemetry.Exporter.InMemory</c> in the integration
    /// test assembly (the package is fine to add, but writing a 6-line exporter keeps the dep
    /// surface small and makes the export contract explicit at the test boundary).</summary>
    private sealed class CollectionExporter : BaseExporter<Activity>
    {
        private readonly InMemorySpanCollector _collector;

        public CollectionExporter(InMemorySpanCollector collector) => _collector = collector;

        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (Activity activity in batch)
            {
                _collector.Activities.Add(activity);
            }

            return ExportResult.Success;
        }
    }
}
