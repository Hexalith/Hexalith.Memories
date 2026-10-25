// <copyright file="AccessTelemetryCapabilityFailureClassifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Dapr;
using Dapr.Client;
using Grpc.Core;

/// <summary>Classifies failures without storing exception objects, messages, endpoints or identity material.</summary>
internal static class AccessTelemetryCapabilityFailureClassifier
{
    /// <summary>Returns only a fixed diagnostic category.</summary>
    public static string Classify(Exception exception) => exception switch
    {
        TimeoutException or OperationCanceledException { InnerException: TimeoutException } => "Timeout",
        OperationCanceledException => "Canceled",
        InvocationException { Response.StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } => "RemoteDenied",
        DaprException { InnerException: RpcException rpc } => Rpc(rpc.StatusCode),
        RpcException rpc => Rpc(rpc.StatusCode),
        DaprException => "DaprApi",
        HttpRequestException or SocketException or IOException => "Transport",
        FormatException or ArgumentException or JsonException or OverflowException => "Format",
        _ => "Other",
    };

    private static string Rpc(StatusCode code) => code switch
    {
        StatusCode.PermissionDenied or StatusCode.Unauthenticated => "RemoteDenied",
        StatusCode.DeadlineExceeded => "Timeout",
        StatusCode.Cancelled => "Canceled",
        StatusCode.Unavailable => "Transport",
        _ => "DaprApi",
    };
}
