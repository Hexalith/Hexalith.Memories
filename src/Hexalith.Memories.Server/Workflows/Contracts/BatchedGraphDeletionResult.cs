// <copyright file="BatchedGraphDeletionResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Result of a single batch of the FalkorDB graph node deletion activity.</summary>
public sealed record BatchedGraphDeletionResult(long RemainingNodes, int DeletedInBatch, bool IsComplete);
