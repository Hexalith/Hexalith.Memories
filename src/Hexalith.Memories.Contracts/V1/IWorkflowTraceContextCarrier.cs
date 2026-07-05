// <copyright file="IWorkflowTraceContextCarrier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Implemented by durable workflow activity inputs that carry serialized request trace context.</summary>
public interface IWorkflowTraceContextCarrier
{
    /// <summary>Gets the serialized trace context captured before crossing the durable workflow boundary.</summary>
    WorkflowTraceContext? TraceContext { get; }
}
