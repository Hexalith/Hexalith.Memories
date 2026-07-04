// <copyright file="WorkflowTestHelpers.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Workflows;

using Dapr.Workflow;

/// <summary>Shared helpers for Dapr workflow orchestration tests.</summary>
internal static class WorkflowTestHelpers
{
    /// <summary>Creates a <see cref="WorkflowTaskFailedException"/> for activity failure injection.
    /// The exception type has non-public constructors, so an uninitialized instance is used —
    /// workflows only react to the exception type, never its message.</summary>
    public static WorkflowTaskFailedException CreateTaskFailedException()
        => (WorkflowTaskFailedException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
            typeof(WorkflowTaskFailedException));
}
