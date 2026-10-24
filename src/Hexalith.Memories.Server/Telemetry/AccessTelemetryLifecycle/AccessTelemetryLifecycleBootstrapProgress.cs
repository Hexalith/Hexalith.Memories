// <copyright file="AccessTelemetryLifecycleBootstrapProgress.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Net;
using System.Net.Sockets;
using System.Text.Json;

using Dapr;
using Dapr.Client;

using Grpc.Core;

/// <summary>
/// Process-local, secret-free bootstrap progress written by <see cref="AccessTelemetryLifecycleBootstrapService"/> and
/// read by qualification diagnostics. It never changes health, metric, status or readiness semantics.
/// </summary>
internal sealed class AccessTelemetryLifecycleBootstrapProgress
{
    private AccessTelemetryLifecycleBootstrapSnapshot _current = AccessTelemetryLifecycleBootstrapSnapshot.NotStarted;

    /// <summary>Gets the immutable current progress.</summary>
    public AccessTelemetryLifecycleBootstrapSnapshot Current => Volatile.Read(ref _current);

    /// <summary>Maps a bootstrap failure to the fixed allow-list without retaining any exception detail.</summary>
    /// <param name="exception">The failure raised by a bootstrap dependency call.</param>
    /// <returns>The bounded failure kind.</returns>
    public static AccessTelemetryLifecycleBootstrapFailureKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            FormatException or ArgumentException or OverflowException or JsonException => AccessTelemetryLifecycleBootstrapFailureKind.Format,
            TimeoutException or OperationCanceledException { InnerException: TimeoutException } => AccessTelemetryLifecycleBootstrapFailureKind.Timeout,
            OperationCanceledException => AccessTelemetryLifecycleBootstrapFailureKind.Canceled,

            // InvocationException derives from DaprException, so it is matched first.
            InvocationException invocation => ClassifyInvocation(invocation),
            RpcException rpc => ClassifyRpc(rpc.StatusCode),
            DaprException { InnerException: RpcException rpc } => ClassifyRpc(rpc.StatusCode),
            DaprException => AccessTelemetryLifecycleBootstrapFailureKind.DaprApi,
            HttpRequestException or SocketException or IOException => AccessTelemetryLifecycleBootstrapFailureKind.Transport,
            _ => AccessTelemetryLifecycleBootstrapFailureKind.Other,
        };
    }

    /// <summary>Marks the start of a new attempt at the configuration stage.</summary>
    public void BeginAttempt() => Enter(AccessTelemetryLifecycleBootstrapStage.Configuration);

    /// <summary>Records the stage now in flight.</summary>
    /// <param name="stage">The stage being entered.</param>
    public void Enter(AccessTelemetryLifecycleBootstrapStage stage)
    {
        string name = Name(stage);
        Update(current => current with { Stage = name });
    }

    /// <summary>Completes the current attempt as failed at its in-flight stage.</summary>
    /// <param name="kind">The bounded failure kind.</param>
    public void CompleteFailure(AccessTelemetryLifecycleBootstrapFailureKind kind)
    {
        string name = Enum.IsDefined(kind) ? kind.ToString() : nameof(AccessTelemetryLifecycleBootstrapFailureKind.Other);
        Update(current => current with
        {
            Attempts = current.Attempts + 1,
            FailureStage = current.Stage,
            FailureKind = name,
        });
    }

    /// <summary>Completes the current attempt with a validated sanitizer published.</summary>
    /// <param name="utcNow">The publication time; only the first publication time is retained.</param>
    public void CompletePublished(DateTimeOffset utcNow)
    {
        long publishedUtcMs = utcNow.ToUnixTimeMilliseconds();
        Update(current => current with
        {
            Attempts = current.Attempts + 1,
            Stage = nameof(AccessTelemetryLifecycleBootstrapStage.Published),
            FirstPublishedUtcMs = current.FirstPublishedUtcMs ?? publishedUtcMs,
        });
    }

    private static AccessTelemetryLifecycleBootstrapFailureKind ClassifyInvocation(InvocationException invocation)
    {
        if (invocation.Response is { } response)
        {
            return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? AccessTelemetryLifecycleBootstrapFailureKind.RemoteDenied
                : response.IsSuccessStatusCode
                    ? AccessTelemetryLifecycleBootstrapFailureKind.Format
                    : AccessTelemetryLifecycleBootstrapFailureKind.DaprApi;
        }

        return invocation.InnerException switch
        {
            RpcException rpc => ClassifyRpc(rpc.StatusCode),
            TimeoutException or OperationCanceledException { InnerException: TimeoutException } => AccessTelemetryLifecycleBootstrapFailureKind.Timeout,
            OperationCanceledException => AccessTelemetryLifecycleBootstrapFailureKind.Canceled,
            HttpRequestException or SocketException or IOException => AccessTelemetryLifecycleBootstrapFailureKind.Transport,
            _ => AccessTelemetryLifecycleBootstrapFailureKind.DaprApi,
        };
    }

    private static AccessTelemetryLifecycleBootstrapFailureKind ClassifyRpc(StatusCode code)
        => code switch
        {
            StatusCode.DeadlineExceeded => AccessTelemetryLifecycleBootstrapFailureKind.Timeout,
            StatusCode.Cancelled => AccessTelemetryLifecycleBootstrapFailureKind.Canceled,
            StatusCode.Unavailable => AccessTelemetryLifecycleBootstrapFailureKind.Transport,

            // Denials of secret, configuration or invocation calls; the failure stage names the denied call.
            StatusCode.PermissionDenied or StatusCode.Unauthenticated => AccessTelemetryLifecycleBootstrapFailureKind.RemoteDenied,
            _ => AccessTelemetryLifecycleBootstrapFailureKind.DaprApi,
        };

    private static string Name(AccessTelemetryLifecycleBootstrapStage stage)
        => Enum.IsDefined(stage) ? stage.ToString() : AccessTelemetryLifecycleBootstrapSnapshot.Unobserved;

    private void Update(Func<AccessTelemetryLifecycleBootstrapSnapshot, AccessTelemetryLifecycleBootstrapSnapshot> change)
    {
        AccessTelemetryLifecycleBootstrapSnapshot current = Volatile.Read(ref _current);
        while (true)
        {
            AccessTelemetryLifecycleBootstrapSnapshot observed = Interlocked.CompareExchange(ref _current, change(current), current);
            if (ReferenceEquals(observed, current))
            {
                return;
            }

            current = observed;
        }
    }
}
