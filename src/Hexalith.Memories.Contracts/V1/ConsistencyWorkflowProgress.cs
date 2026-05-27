// <copyright file="ConsistencyWorkflowProgress.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Progress snapshot exposed by the consistency workflow status endpoints.
/// </summary>
/// <param name="CurrentPhase">Current workflow phase (for example: <c>enumerating</c>, <c>verifying</c>, <c>repairing</c>, <c>completed</c>).</param>
/// <param name="BatchesProcessed">Number of completed batches in the current phase.</param>
/// <param name="TotalBatches">Total batches expected for the current phase.</param>
public sealed record ConsistencyWorkflowProgress(
    string CurrentPhase,
    int BatchesProcessed,
    int TotalBatches);