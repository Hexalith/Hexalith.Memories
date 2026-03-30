// <copyright file="IdempotencyInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Input for the idempotency check activity.</summary>
/// <param name="SourceUri">The URI of the source content.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="CaseId">The case identifier.</param>
public sealed record IdempotencyInput(string SourceUri, string TenantId, string CaseId);
