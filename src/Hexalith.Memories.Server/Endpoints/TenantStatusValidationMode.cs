// <copyright file="TenantStatusValidationMode.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

/// <summary>Controls whether the tenant-state endpoint filter requires an active tenant or only existence.</summary>
internal enum TenantStatusValidationMode
{
    /// <summary>Require the tenant to exist and be active.</summary>
    ActiveOnly,

    /// <summary>Require the tenant to exist, but allow non-active lifecycle states.</summary>
    ExistsOnly,
}
