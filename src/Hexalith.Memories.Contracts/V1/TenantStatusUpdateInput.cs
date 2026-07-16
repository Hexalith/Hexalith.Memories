// <copyright file="TenantStatusUpdateInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Input for the update-tenant-status activity.</summary>
public sealed record TenantStatusUpdateInput(string TenantId, TenantStatus Status, string? WorkflowInstanceId = null);
