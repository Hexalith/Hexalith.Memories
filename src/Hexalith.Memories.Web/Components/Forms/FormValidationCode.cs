// <copyright file="FormValidationCode.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

/// <summary>
/// Machine-readable contract-aware form validation outcomes.
/// </summary>
/// <remarks>
/// Story 17.3 (AC1) — every code maps through <see cref="FormValidationTraceability"/> to a severity, a
/// dispatch classification, a localization key, and the named contract field that justifies it, so the
/// validation vocabulary stays traceable to the upstream contract instead of ad-hoc component strings.
/// </remarks>
public enum FormValidationCode
{
    /// <summary>The tenant scope identifier is missing.</summary>
    TenantRequired = 0,

    /// <summary>A required case scope identifier is missing.</summary>
    CaseRequired,

    /// <summary>A required field is empty.</summary>
    FieldRequired,

    /// <summary>A contract-enum field carries a value outside the known token set.</summary>
    UnknownEnumValue,

    /// <summary>A numeric field is non-numeric or outside its inclusive range.</summary>
    ValueOutOfRange,

    /// <summary>The requested scope is unauthorized or its isolation status cannot be trusted.</summary>
    UnauthorizedScope,

    /// <summary>The submission changes the tenant relative to the current scope.</summary>
    TenantChange,

    /// <summary>The submission broadens scope from a specific case to tenant-wide.</summary>
    ScopeBroadened,

    /// <summary>The submission enables a dangerous toggle (such as a forced repair).</summary>
    DangerousChange,
}
