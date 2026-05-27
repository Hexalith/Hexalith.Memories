// <copyright file="TenantRegistryEntry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using Hexalith.Memories.Contracts.V1;

/// <summary>Internal tenant registry state persisted in DAPR, including workflow ownership for in-flight provisioning.</summary>
public sealed record TenantRegistryEntry(TenantInfo Tenant, string? WorkflowInstanceId, DateTimeOffset LastUpdated = default);