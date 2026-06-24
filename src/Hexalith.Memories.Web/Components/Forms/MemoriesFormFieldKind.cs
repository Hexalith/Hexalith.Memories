// <copyright file="MemoriesFormFieldKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

/// <summary>
/// Validation kind for a single contract-aware form field.
/// </summary>
/// <remarks>
/// Story 17.3 (AC1) — contract-aware validation means each field declares how it must be validated against
/// the typed contract (scope identifiers, enum tokens, numeric ranges) rather than relying on hand-written
/// string lists hidden in the component.
/// </remarks>
public enum MemoriesFormFieldKind
{
    /// <summary>The tenant-scope identifier. Always required and always rendered first.</summary>
    TenantScope = 0,

    /// <summary>The case-scope identifier. Required for case-scoped forms; optional for tenant-wide forms.</summary>
    CaseScope,

    /// <summary>Free text that must be present.</summary>
    RequiredText,

    /// <summary>Free text that may be empty.</summary>
    OptionalText,

    /// <summary>A value that must be one of a known set of contract enum tokens.</summary>
    ContractEnum,

    /// <summary>A numeric value that must parse and fall within an inclusive range.</summary>
    NumericRange,

    /// <summary>A boolean toggle whose <c>true</c> value marks a dangerous change requiring acknowledgement.</summary>
    DangerousToggle,
}
