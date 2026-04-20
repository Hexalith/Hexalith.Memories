// <copyright file="ExportScope.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Identifies the scope of a data export produced by <c>TenantExportService</c> (Story 8.3).</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<ExportScope>))]
public enum ExportScope
{
    /// <summary>Export covers a single case in a tenant.</summary>
    Case,

    /// <summary>Export covers an entire tenant (all cases, all memory units, all edges, tenant configuration).</summary>
    Tenant,
}
