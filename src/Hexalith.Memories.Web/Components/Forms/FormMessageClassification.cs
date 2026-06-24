// <copyright file="FormMessageClassification.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Forms;

/// <summary>
/// How a <see cref="FormValidationMessage"/> affects dispatch.
/// </summary>
/// <remarks>
/// Story 17.3 (AC1) — distinguishes hard validation errors (which block submission) from dangerous changes
/// (which submission is allowed only after explicit acknowledgement) from advisory notes (which never
/// block). Keeping these separate lets the UI block inconsistent changes while still allowing an informed
/// user to proceed with a dangerous-but-valid scope change.
/// </remarks>
public enum FormMessageClassification
{
    /// <summary>Informational only; does not block dispatch and needs no acknowledgement.</summary>
    Advisory = 0,

    /// <summary>A dangerous-but-valid change; dispatch requires explicit acknowledgement.</summary>
    Acknowledgement,

    /// <summary>A hard validation error; dispatch is blocked until corrected.</summary>
    Blocking,
}
