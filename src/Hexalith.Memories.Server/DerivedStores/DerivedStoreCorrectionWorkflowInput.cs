// <copyright file="DerivedStoreCorrectionWorkflowInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

/// <summary>Identifies one deterministic durable correction workflow.</summary>
internal sealed record DerivedStoreCorrectionWorkflowInput(string TenantId, string OperationId);
