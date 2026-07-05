// <copyright file="WorkflowTraceContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Serialized W3C trace context captured before a durable workflow boundary.</summary>
public sealed record WorkflowTraceContext
{
    /// <summary>Gets the W3C <c>traceparent</c> header value.</summary>
    [JsonPropertyName("traceparent")]
    public required string TraceParent { get; init; }

    /// <summary>Gets the optional W3C <c>tracestate</c> header value.</summary>
    [JsonPropertyName("tracestate")]
    public string? TraceState { get; init; }
}
