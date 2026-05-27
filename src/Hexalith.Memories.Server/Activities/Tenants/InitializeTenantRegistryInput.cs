// <copyright file="InitializeTenantRegistryInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Tenants;

/// <summary>Input for initializing tenant registry state with workflow ownership information.</summary>
public sealed record InitializeTenantRegistryInput(string TenantId, string DisplayName, string WorkflowInstanceId);