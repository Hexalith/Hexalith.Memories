// <copyright file="WorkflowPayloadException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

/// <summary>Structured non-secret exception raised when a claim-checked workflow payload cannot be used.</summary>
public sealed class WorkflowPayloadException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="WorkflowPayloadException"/> class.</summary>
    public WorkflowPayloadException(string errorCode, string payloadId)
        : base($"Workflow payload '{payloadId}' failed validation: {errorCode}.")
    {
        ErrorCode = errorCode;
        PayloadId = payloadId;
    }

    /// <summary>Gets the stable error code.</summary>
    public string ErrorCode { get; }

    /// <summary>Gets the payload reference identifier.</summary>
    public string PayloadId { get; }
}
