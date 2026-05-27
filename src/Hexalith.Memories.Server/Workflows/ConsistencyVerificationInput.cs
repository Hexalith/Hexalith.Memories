// <copyright file="ConsistencyVerificationInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

/// <summary>Input for <c>ConsistencyVerificationWorkflow</c>.</summary>
/// <param name="TenantId">The tenant to audit.</param>
/// <param name="BatchSize">Per-batch fan-out size (must be in [10, 5000]).</param>
public sealed record ConsistencyVerificationInput(string TenantId, int BatchSize = 500);
