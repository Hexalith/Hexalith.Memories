// <copyright file="MemoriesCommandInvocation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>Command activation intent emitted by Story 17.3 command surfaces.</summary>
/// <param name="Kind">The selected command.</param>
/// <param name="TenantId">The tenant scope.</param>
/// <param name="CaseId">The case scope, or null for tenant-wide.</param>
/// <param name="Target">The bounded target object.</param>
/// <param name="ReturnRoute">The preserved return route.</param>
public sealed record MemoriesCommandInvocation(
    MemoriesCommandKind Kind,
    string TenantId,
    string? CaseId,
    string Target,
    string ReturnRoute);
