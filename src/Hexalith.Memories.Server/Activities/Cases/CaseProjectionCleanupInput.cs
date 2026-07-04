// <copyright file="CaseProjectionCleanupInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

/// <summary>Input for case projection cleanup.</summary>
internal sealed record CaseProjectionCleanupInput(string TenantId, string CaseId);
