// <copyright file="ConsistencyCommandReceipt.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Commands;

/// <summary>
/// Fire-and-forget receipt returned by <c>memories consistency verify</c> and
/// <c>memories consistency repair</c> when <c>--wait</c> is not supplied. Carries the
/// workflow instance id so operators can poll status separately.
/// </summary>
/// <param name="TenantId">The tenant the workflow was scheduled for.</param>
/// <param name="WorkflowInstanceId">The workflow instance id (used to poll status).</param>
/// <param name="Kind"><c>verify</c> or <c>repair</c> — identifies the workflow variant.</param>
/// <param name="StatusUrl">Absolute or relative workflow-status URL returned by the server.</param>
public sealed record ConsistencyCommandReceipt(
    string TenantId,
    string WorkflowInstanceId,
    string Kind,
    Uri StatusUrl);
