// <copyright file="DerivedStoreCorrectionStartResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

using Hexalith.Memories.Contracts.V1.DerivedStores;

/// <summary>Reports the durable start-or-rejoin result and whether scheduling is required.</summary>
internal sealed record DerivedStoreCorrectionStartResult(
    DerivedStoreCorrectionStatus Status,
    bool ShouldSchedule,
    string WorkflowInstanceId);
