// <copyright file="CaseDeletionProjectionInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

/// <summary>Input for case deletion projection cleanup.</summary>
internal sealed record CaseDeletionProjectionInput(string TenantId, string CaseId, IReadOnlyList<string> MemoryUnitIds);
